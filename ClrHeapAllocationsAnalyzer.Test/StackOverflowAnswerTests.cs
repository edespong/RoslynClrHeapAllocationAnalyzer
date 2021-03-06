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

using System.Collections.Immutable;
using ClrHeapAllocationAnalyzer.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClrHeapAllocationAnalyzer.Test
{
    /// <summary>
    /// Taken from http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp
    /// </summary>
    [TestClass]
    public class StackOverflowAnswerTests : AllocationAnalyzerTests
    {
        [TestMethod]
        public void Converting_any_value_type_to_System_Object_type()
        {
            var @script =
                    @"struct S { }
                    object box = new S();";
            var analyser = new ExplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, @script, ImmutableArray.Create(SyntaxKind.ObjectCreationExpression, SyntaxKind.AnonymousObjectCreationExpression, SyntaxKind.ArrayInitializerExpression, SyntaxKind.CollectionInitializerExpression, SyntaxKind.ComplexElementInitializerExpression, SyntaxKind.ObjectInitializerExpression, SyntaxKind.ArrayCreationExpression, SyntaxKind.ImplicitArrayCreationExpression, SyntaxKind.LetClause));
            Assert.AreEqual(1, info.Allocations.Count);
            // Diagnostic: (2,34): info HeapAnalyzerExplicitNewObjectRule: Explicit new reference type allocation
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.NewObjectRule.Id, line: 2, character: 34);
        }

        [TestMethod]
        public void Converting_any_value_type_to_System_ValueType_type()
        {
            var @script =
                    @"struct S { }
                    System.ValueType box = new S();";
            var analyser = new ExplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, @script, ImmutableArray.Create(SyntaxKind.ObjectCreationExpression, SyntaxKind.AnonymousObjectCreationExpression, SyntaxKind.ArrayInitializerExpression, SyntaxKind.CollectionInitializerExpression, SyntaxKind.ComplexElementInitializerExpression, SyntaxKind.ObjectInitializerExpression, SyntaxKind.ArrayCreationExpression, SyntaxKind.ImplicitArrayCreationExpression, SyntaxKind.LetClause));
            Assert.AreEqual(1, info.Allocations.Count);
            // Diagnostic: (2,44): info HeapAnalyzerExplicitNewObjectRule: Explicit new reference type allocation
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.NewObjectRule.Id, line: 2, character: 44);
        }

        [TestMethod]
        public void Converting_any_enumeration_type_to_System_Enum_type()
        {
            var @script =
                @"enum E { A }
                System.Enum box = E.A;";
            var analyser = new TypeConversionAllocationAnalyzer();
            var info = ProcessCode(analyser, @script, ImmutableArray.Create(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.ReturnStatement,
                SyntaxKind.YieldReturnStatement,
                SyntaxKind.CastExpression,
                SyntaxKind.AsExpression,
                SyntaxKind.CoalesceExpression,
                SyntaxKind.ConditionalExpression,
                SyntaxKind.ForEachStatement,
                SyntaxKind.EqualsValueClause,
                SyntaxKind.Argument));
            Assert.AreEqual(1, info.Allocations.Count);
            // Diagnostic: (2,35): warning HeapAnalyzerBoxingRule: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.ValueTypeToReferenceTypeConversionRule.Id, line: 2, character: 35);
        }

        [TestMethod]
        public void Converting_any_value_type_into_interface_reference()
        {
            var @script =
                @"interface I { }
                struct S : I { }
                I box = new S();";
            var analyser = new ExplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, @script, ImmutableArray.Create(SyntaxKind.ObjectCreationExpression, SyntaxKind.AnonymousObjectCreationExpression, SyntaxKind.ArrayInitializerExpression, SyntaxKind.CollectionInitializerExpression, SyntaxKind.ComplexElementInitializerExpression, SyntaxKind.ObjectInitializerExpression, SyntaxKind.ArrayCreationExpression, SyntaxKind.ImplicitArrayCreationExpression, SyntaxKind.LetClause));
            Assert.AreEqual(1, info.Allocations.Count);
            // Diagnostic: (3,25): info HeapAnalyzerExplicitNewObjectRule: Explicit new reference type allocation
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.NewObjectRule.Id, line: 3, character: 25);
        }

        [TestMethod]
        public void Non_constant_value_types_in_CSharp_string_concatenation()
        {
            var @script =
                @"System.DateTime c = System.DateTime.Now;;
                string s1 = ""char value will box"" + c;";
            var analyser = new ConcatenationAllocationAnalyzer();
            var info = ProcessCode(analyser, @script, ImmutableArray.Create(SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression));
            Assert.AreEqual(1, info.Allocations.Count);
            //Diagnostic: (2,53): warning HeapAnalyzerBoxingRule: Value type (char) is being boxed to a reference type for a string concatenation.
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.ValueTypeToReferenceTypeInAStringConcatenationRule.Id, line: 2, character: 53);
        }

        [TestMethod]
        public void Creating_delegate_from_value_type_instance_method()
        {
            var @script =
                @"using System;
                struct S { public void M() {} }
                Action box = new S().M;";
            var analyser = new TypeConversionAllocationAnalyzer();
            var info = ProcessCode(analyser, @script, ImmutableArray.Create(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.ReturnStatement,
                SyntaxKind.YieldReturnStatement,
                SyntaxKind.CastExpression,
                SyntaxKind.AsExpression,
                SyntaxKind.CoalesceExpression,
                SyntaxKind.ConditionalExpression,
                SyntaxKind.ForEachStatement,
                SyntaxKind.EqualsValueClause,
                SyntaxKind.Argument));
            Assert.AreEqual(2, info.Allocations.Count);
            // Diagnostic: (3,30): warning HeapAnalyzerMethodGroupAllocationRule: This will allocate a delegate instance
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.MethodGroupAllocationRule.Id, line: 3, character: 30);
            // Diagnostic: (3,30): warning HeapAnalyzerDelegateOnStructRule: Struct instance method being used for delegate creation, this will result in a boxing instruction
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.DelegateOnStructInstanceRule.Id, line: 3, character: 30);
        }

        [TestMethod]
        public void Calling_non_overridden_virtual_methods_on_value_types()
        {
            var @script =
                @"enum E { A }
                E.A.GetHashCode();";
            var analyser = new CallSiteImplicitAllocationAnalyzer();
            var info = ProcessCode(analyser, @script, ImmutableArray.Create(SyntaxKind.InvocationExpression));
            Assert.AreEqual(1, info.Allocations.Count);
            // Diagnostic: (2,17): warning HeapAnalyzerValueTypeNonOverridenCallRule: Non-overriden virtual method call on a value type adds a boxing or constrained instruction
            AssertEx.ContainsDiagnostic(info.Allocations, AllocationRules.ValueTypeNonOverridenCallRule.Id, line: 2, character: 17);
        }
    }
}
