using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClrHeapAllocationAnalyzer.Test
{
    [TestClass]
    public class CallSiteImplicitAllocationAnalyzerTests : AllocationAnalyzerTests
    {
        [TestMethod]
        public void CallSiteImplicitAllocation_Param()
        {
            var sampleProgram =
@"using System;

Params();
Params(1, 2);
Params(new [] { 1, 2}); // explicit, so no warning
ParamsWithObjects(new [] { 1, 2}); // explicit, but converted to objects, so stil la warning?!

// Only 4 args and above use the params overload of String.Format
var test = String.Format(""Testing {0}, {1}, {2}, {3}"", 1, ""blah"", 2.0m, 'c');

public void Params(params int[] args)
{
}

public void ParamsWithObjects(params object[] args)
{
}";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, sampleProgram, ImmutableArray.Create(SyntaxKind.InvocationExpression));

            Assert.AreEqual(4, info.Allocations.Count);
            // Diagnostic: (3,1): warning HeapAnalyzerImplicitParamsRule: This call site is calling into a function with a 'params' parameter. This results in an array allocation even if no parameter is passed in for the params parameter
            AssertEx.ContainsDiagnostic(info.Allocations, id: CallSiteImplicitAllocationAnalyzer.ParamsParameterRule.Id, line: 3, character: 1);
            // Diagnostic: (4,1): warning HeapAnalyzerImplicitParamsRule: This call site is calling into a function with a 'params' parameter. This results in an array allocation even if no parameter is passed in for the params parameter
            AssertEx.ContainsDiagnostic(info.Allocations, id: CallSiteImplicitAllocationAnalyzer.ParamsParameterRule.Id, line: 4, character: 1);
            // Diagnostic: (6,1): warning HeapAnalyzerImplicitParamsRule: This call site is calling into a function with a 'params' parameter. This results in an array allocation even if no parameter is passed in for the params parameter
            AssertEx.ContainsDiagnostic(info.Allocations, id: CallSiteImplicitAllocationAnalyzer.ParamsParameterRule.Id, line: 6, character: 1);
            // Diagnostic: (9,12): warning HeapAnalyzerImplicitParamsRule: This call site is calling into a function with a 'params' parameter. This results in an array allocation even if no parameter is passed in for the params parameter
            AssertEx.ContainsDiagnostic(info.Allocations, id: CallSiteImplicitAllocationAnalyzer.ParamsParameterRule.Id, line: 9, character: 12);
        }

        [TestMethod]
        public void CallSiteImplicitAllocation_NonOverridenMethodOnStruct() {
            var sampleProgram =
                @"using System;

var normal = new Normal().GetHashCode();
var overridden = new OverrideToHashCode().GetHashCode();

struct Normal
{

}

struct OverrideToHashCode
{

    public override int GetHashCode()
    {
        return -1;
    }
}";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, sampleProgram, ImmutableArray.Create(SyntaxKind.InvocationExpression));

            Assert.AreEqual(1, info.Allocations.Count);
            // Diagnostic: (3,14): warning HeapAnalyzerValueTypeNonOverridenCallRule: Non-overriden virtual method call on a value type adds a boxing or constrained instruction
            AssertEx.ContainsDiagnostic(info.Allocations, id: CallSiteImplicitAllocationAnalyzer.ValueTypeNonOverridenCallRule.Id, line: 3, character: 14);
        }

        [TestMethod]
        public void CallSiteImplicitAllocation_DoNotReportNonOverriddenMethodCallForStaticCalls() {
            var snippet = @"var t = System.Enum.GetUnderlyingType(typeof(System.StringComparison));";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.InvocationExpression));

            Assert.AreEqual(0, info.Allocations.Count);
          }

        [TestMethod]
        public void CallSiteImplicitAllocation_DoNotReportNonOverriddenMethodCallForNonVirtualCalls() {
            var snippet = @"
using System.IO;

FileAttributes attr = FileAttributes.System;
attr.HasFlag (FileAttributes.Directory);
";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.InvocationExpression));

            Assert.AreEqual(0, info.Allocations.Count);
        }

        [TestMethod]
        public void CallSiteImplicitAllocation_DictionaryWithEnumKeyNoComparer_ReportsAllocation() {
            var snippet = @"
using System;
using System.Collections.Generic;

Dictionary<DayOfWeek, int> foo = new Dictionary<DayOfWeek, int>();
";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.InvocationExpression)); // TODO: Wrong SyntaxKind

            Assert.AreEqual(1, info.Allocations.Count);
        }
        
        [TestMethod]
        public void CallSiteImplicitAllocation_DictionaryWithNonEquatableStructKeyNoComparer_ReportsAllocation() {
            var snippet = @"
using System;
using System.Collections.Generic;

struct MyStruct { }

Dictionary<MyStruct, int> foo = new Dictionary<MyStruct, int>();
";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.InvocationExpression)); // TODO: Wrong SyntaxKind

            Assert.AreEqual(1, info.Allocations.Count);
        }
        
        [TestMethod]
        public void CallSiteImplicitAllocation_DictionaryWithEquatableStructKey_NoReport() {
            var snippet = @"
using System;
using System.Collections.Generic;

struct MyStruct : IEquatable<MyStruct>
{
    public bool Equals(MyStruct other) { return false; }
}

Dictionary<MyStruct, int> foo = new Dictionary<MyStruct, int>();
";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.InvocationExpression)); // TODO: Wrong SyntaxKind

            Assert.AreEqual(0, info.Allocations.Count);
        }

        [TestMethod]
        public void CallSiteImplicitAllocation_DictionaryWithEnumKeyWithComparer_NoReport() {
            var snippet = @"
using System;
using System.Collections.Generic;

public class DayOfWeekComparer : IEqualityComparer<DayOfWeek>
{
    public bool Equals(DayOfWeek x, DayOfWeek y) { return false; }
}

Dictionary<DayOfWeek, int> foo = new Dictionary<DayOfWeek, int>(new DayOfWeekComparer());
";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.InvocationExpression)); // TODO: Wrong SyntaxKind

            Assert.AreEqual(0, info.Allocations.Count);
        }

        [TestMethod]
        public void CallSiteImplicitAllocation_DictionaryWithStructKeyWithComparer_NoReport() {
            var snippet = @"
using System;
using System.Collections.Generic;

struct MyStruct : IEquatable<MyStruct> { }

public class MyStructComparer : IEqualityComparer<MyStruct>
{
    public bool Equals(MyStruct x, MyStruct y) { return false; }
}

Dictionary<MyStruct, int> foo = new Dictionary<MyStruct, int>(new MyStructComparer());
";

            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, snippet, ImmutableArray.Create(SyntaxKind.InvocationExpression)); // TODO: Wrong SyntaxKind

            Assert.AreEqual(0, info.Allocations.Count);
        }
    }
}
