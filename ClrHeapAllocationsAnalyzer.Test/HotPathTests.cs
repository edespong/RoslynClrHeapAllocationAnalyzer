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

namespace ClrHeapAllocationAnalyzer.Test
{
    [TestClass]
    public class HotPathTests : AllocationAnalyzerTests
    {
        [TestMethod]
        public void AnalyzeProgram_MethodWithAttribute_OtherMethodIgnored()
        {
            const string sampleProgram =
                @"using System;
                using Microsoft.Diagnostics;
                
                [PerformanceSensitive]
                public void CreateString1() {
                    string str = new string('a', 5);
                }

                public void CreateString2() {
                    string str = new string('b', 5);
                }";

            var analyser = new ExplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, sampleProgram, ImmutableArray.Create(SyntaxKind.ObjectInitializerExpression));
            Assert.AreEqual(1, info.Allocations.Count);
            AssertEx.ContainsDiagnostic(info.Allocations, id: AllocationRules.NewObjectRule.Id, line: 6);
        }

        [TestMethod]
        public void AnalyzeProgram_MethodsWithAttribute_BothAnalyzed()
        {
            const string sampleProgram =
                @"using System;
                using Microsoft.Diagnostics;
                
                [PerformanceSensitive]
                public void CreateString1() {
                    string str = new string('a', 5);
                }
                
                [PerformanceSensitive]
                public void CreateString2() {
                    string str = new string('a', 5);
                }";

            var analyser = new ExplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, sampleProgram, ImmutableArray.Create(SyntaxKind.ObjectInitializerExpression));
            Assert.AreEqual(2, info.Allocations.Count);
            AssertEx.ContainsDiagnostic(info.Allocations, id: AllocationRules.NewObjectRule.Id, line: 6);
            AssertEx.ContainsDiagnostic(info.Allocations, id: AllocationRules.NewObjectRule.Id, line: 11);
        }
    }
}
