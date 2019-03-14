﻿/* -----------------------------------------------------------------------------
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using ClrHeapAllocationAnalyzer.Common;

namespace ClrHeapAllocationAnalyzer.Test
{
    [TestClass]
    public class ConcatenationAllocationAnalyzerTests : AllocationAnalyzerTests
    {
        [TestMethod]
        public void ConcatenationAllocation_Basic()
        {
            var snippet0 = @"string s0 = ""hello"" + 0.ToString() + ""world"" + 1.ToString();";
            var snippet1 = @"string s2 = ""ohell"" + 2.ToString() + ""world"" + 3.ToString() + 4.ToString();";

            var analyser = new ConcatenationAllocationAnalyzer();
            var info0 = ProcessCode(analyser, snippet0, ImmutableArray.Create(SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression));
            var info1 = ProcessCode(analyser, snippet1, ImmutableArray.Create(SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression));

            Assert.AreEqual(0, info0.Allocations.Count(d => d.Id == AllocationRules.StringConcatenationAllocationRule.Id));
            Assert.AreEqual(1, info1.Allocations.Count(d => d.Id == AllocationRules.StringConcatenationAllocationRule.Id));
            AssertEx.ContainsDiagnostic(info1.Allocations, id: AllocationRules.StringConcatenationAllocationRule.Id, line: 1, character: 13);
        }

        [TestMethod]
        public void ConcatenationAllocation_DoNotWarnForOptimizedValueTypes()
        {
            var snippets = new[]
            {
                @"string s0 = nameof(System.String) + '-';",
                @"string s0 = nameof(System.String) + true;",
                @"string s0 = nameof(System.String) + new System.IntPtr();",
                @"string s0 = nameof(System.String) + new System.UIntPtr();"
            };

            var analyser = new ConcatenationAllocationAnalyzer();
            foreach (var snippet in snippets)
            {
                var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression));
                Assert.AreEqual(0, info.Allocations.Count(x => x.Id == AllocationRules.ValueTypeToReferenceTypeInAStringConcatenationRule.Id));
            }
        }

        [TestMethod]
        public void ConcatenationAllocation_DoNotWarnForConst()
        {
            var snippets = new[]
            {
                @"const string s0 = nameof(System.String) + ""."" + nameof(System.String);",
                @"const string s0 = nameof(System.String) + ""."";",
                @"string s0 = nameof(System.String) + ""."" + nameof(System.String);",
                @"string s0 = nameof(System.String) + ""."";"
            };

            var analyser = new ConcatenationAllocationAnalyzer();
            foreach (var snippet in snippets)
            {
                var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression));
                Assert.AreEqual(0, info.Allocations.Count);
            }
        }
    }
}
