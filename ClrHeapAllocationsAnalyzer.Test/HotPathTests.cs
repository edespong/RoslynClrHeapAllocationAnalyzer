/* -----------------------------------------------------------------------------
 *  Licenced under the Apache License, Version 2.0. See LICENSE in the project
 *  root for licence information.
 *  
 *  The content of this file has been forked from the Clr Heap Allocation
 *  Analyzer project developed by Microsoft at 
 *  https://github.com/Microsoft/RoslynClrHeapAllocationAnalyzer and contains,
 *  sometimes considerable, changes in functionality by Erik Edespong. For more
 *  specific information regarding these, see the NOTICE file in the project
 *  root.
 * ---------------------------------------------------------------------------*/

using ClrHeapAllocationAnalyzer.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.Linq;

namespace ClrHeapAllocationAnalyzer.Test
{
    [TestClass]
    public class HotPathTests : AllocationAnalyzerTests
    {
        private ExplicitAllocationAnalyzer analyzer;
        private readonly ImmutableArray<SyntaxKind> expectedSyntaxes = ImmutableArray.Create(SyntaxKind.ObjectInitializerExpression);
        private const string sampleProgramTemplate = @"
using System;
{0} // Potential attribute
class A
{{
    {1}  // Potential attribute
    public void Method1() {{ string str = new string('1', 5); }}
    {2}  // Potential attribute
    public void Method2() {{ string str = new string('2', 5); }}
}}

{3} // Potential attribute
class B
{{
    {4} // Potential attribute
    public void Method3() {{ string str = new string('3', 5); }}
}}";

        [TestInitialize]
        public void InitializeHotPathTests()
        {
            analyzer = new ExplicitAllocationAnalyzer();
            AllocationRules.Settings.IgnoredAttributes = ""; // Explicitly set to remove any default.
            AllocationRules.Settings.HotPathAttributes = ""; // Explicitly set to remove any default.
            AllocationRules.Settings.OnlyReportOnHotPath = false;
        }

        [TestMethod]
        public void HotPath_NoAttributesAllPaths_AllAllocationsReported()
        {
            var info = Analyze("", "", "", "", "");
            Assert.AreEqual(3, info.Allocations.Count);
            AssertDiagnostic(info, 1, 2, 3);
        }

        [TestMethod]
        public void HotPath_NoAttributeOnlyHotPaths_NothingReported()
        {
            AllocationRules.Settings.HotPathAttributes = "System.ObsoleteAttribute";
            AllocationRules.Settings.OnlyReportOnHotPath = true;
            var info = Analyze("", "", "", "", "");
            Assert.AreEqual(0, info.Allocations.Count);
        }

        [TestMethod]
        public void HotPath_ClassAAttributeAllPaths_AllAllocationsReported()
        {
            AllocationRules.Settings.HotPathAttributes = "System.ObsoleteAttribute";
            var info = Analyze("[System.ObsoleteAttribute]", "", "", "", "");
            Assert.AreEqual(3, info.Allocations.Count);
            AssertDiagnostic(info, 1, 2, 3);
        }

        [TestMethod]
        public void HotPath_ClassAAttributeHotPaths_AllAAllocationsReported()
        {
            AllocationRules.Settings.HotPathAttributes = "System.ObsoleteAttribute";
            AllocationRules.Settings.OnlyReportOnHotPath = true;
            var info = Analyze("[System.ObsoleteAttribute]", "", "", "", "");
            Assert.AreEqual(2, info.Allocations.Count);
            AssertDiagnostic(info, 1, 2);
        }

        [TestMethod]
        public void HotPath_Method1and3AttributeAllPaths_AllocationsReported()
        {
            AllocationRules.Settings.HotPathAttributes = "System.ObsoleteAttribute";
            var info = Analyze("", "[System.ObsoleteAttribute]", "", "", "[System.ObsoleteAttribute]");
            Assert.AreEqual(2, info.Allocations.Count);
            AssertDiagnostic(info, 1, 3);
        }

        [TestMethod]
        public void HotPath_Method1AttributeAllPaths_OnlyHotPathInAReportedFromAButEverythingReportedFromB()
        {
            AllocationRules.Settings.HotPathAttributes = "System.ObsoleteAttribute";
            var info = Analyze("", "[System.ObsoleteAttribute]", "", "", "");
            Assert.AreEqual(2, info.Allocations.Count);
            AssertDiagnostic(info, 1, 3);
        }

        [TestMethod]
        public void HotPath_Method1and3AttributeAllPaths_OnlyHotPathInAandBReported()
        {
            AllocationRules.Settings.HotPathAttributes = "System.ObsoleteAttribute, System.Runtime.CompilerServices.CompilerGeneratedAttribute";
            var info = Analyze("", "[System.ObsoleteAttribute]", "", "", "[System.Runtime.CompilerServices.CompilerGeneratedAttribute]");
            Assert.AreEqual(2, info.Allocations.Count);
            AssertDiagnostic(info, 1, 3);
        }

        private Info Analyze(string classAAttribute, string method1Attribute, string method2Attribute,
            string classBAttribute, string method3Attribute)
        {
            string program = string.Format(sampleProgramTemplate,
                classAAttribute, method1Attribute, method2Attribute, classBAttribute, method3Attribute);
            return ProcessCode(analyzer, program, expectedSyntaxes);
        }

        private static void AssertDiagnostic(Info info, params int[] i)
        {
            void AssertDiagnostic(int line)
            {
                AssertEx.ContainsDiagnostic(info.Allocations, id: AllocationRules.NewObjectRule.Id, line: line, character: 42);
            }
            if (i.Contains(1)) AssertDiagnostic(7);
            if (i.Contains(2)) AssertDiagnostic(9);
            if (i.Contains(3)) AssertDiagnostic(16);
        }
    }
}

