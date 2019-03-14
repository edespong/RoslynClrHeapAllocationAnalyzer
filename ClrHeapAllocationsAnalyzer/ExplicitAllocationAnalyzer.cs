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

namespace ClrHeapAllocationAnalyzer
{
    using System;
    using System.Collections.Immutable;
    using ClrHeapAllocationAnalyzer.Common;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExplicitAllocationAnalyzer : AllocationAnalyzer
    {
        protected override SyntaxKind[] Expressions => new[] {
            SyntaxKind.ObjectCreationExpression,            // Used
            SyntaxKind.AnonymousObjectCreationExpression,   // Used
            SyntaxKind.ArrayInitializerExpression,          // Used (this is inside an ImplicitArrayCreationExpression)
            SyntaxKind.CollectionInitializerExpression,     // Is this used anywhere?
            SyntaxKind.ComplexElementInitializerExpression, // Is this used anywhere? For what this is see http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Compilation/CSharpSemanticModel.cs,80
            SyntaxKind.ObjectInitializerExpression,         // Used linked to InitializerExpressionSyntax
            SyntaxKind.ArrayCreationExpression,             // Used
            SyntaxKind.ImplicitArrayCreationExpression,     // Used (this then contains an ArrayInitializerExpression)
            SyntaxKind.LetClause                            // Used
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                AllocationRules.GetDescriptor(AllocationRules.LetCauseRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.InitializerCreationRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.ImplicitArrayCreationRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.AnonymousNewObjectRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.NewObjectRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.NewArrayRule.Id)
            );

        private static readonly object[] EmptyMessageArgs = { };

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, EnabledRules rules)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            var cancellationToken = context.CancellationToken;
            string filePath = node.SyntaxTree.FilePath;

            // An InitializerExpressionSyntax has an ObjectCreationExpressionSyntax as it's parent, i.e
            // var testing = new TestClass { Name = "Bob" };
            //               |             |--------------| <- InitializerExpressionSyntax or SyntaxKind.ObjectInitializerExpression
            //               |----------------------------| <- ObjectCreationExpressionSyntax or SyntaxKind.ObjectCreationExpression
            if (rules.TryGet(AllocationRules.InitializerCreationRule.Id, out var creationRule))
            {
                var initializerExpression = node as InitializerExpressionSyntax;
                if (initializerExpression?.Parent is ObjectCreationExpressionSyntax)
                {
                    var objectCreation = node.Parent as ObjectCreationExpressionSyntax;
                    var typeInfo = semanticModel.GetTypeInfo(objectCreation, cancellationToken);
                    if (typeInfo.ConvertedType?.TypeKind != TypeKind.Error &&
                        typeInfo.ConvertedType?.IsReferenceType == true &&
                        objectCreation.Parent?.IsKind(SyntaxKind.EqualsValueClause) == true &&
                        objectCreation.Parent?.Parent?.IsKind(SyntaxKind.VariableDeclarator) == true)
                    {
                        reportDiagnostic(Diagnostic.Create(creationRule, ((VariableDeclaratorSyntax)objectCreation.Parent.Parent).Identifier.GetLocation(), EmptyMessageArgs));
                        return;
                    }
                }
            }

            if (rules.TryGet(AllocationRules.ImplicitArrayCreationRule.Id, out var arrayCreationRule))
            {
                if (node is ImplicitArrayCreationExpressionSyntax implicitArrayExpression)
                {
                    reportDiagnostic(Diagnostic.Create(arrayCreationRule, implicitArrayExpression.NewKeyword.GetLocation(), EmptyMessageArgs));
                    return;
                }
            }

            if (rules.TryGet(AllocationRules.AnonymousNewObjectRule.Id, out var anonCreationRule))
            {
                if (node is AnonymousObjectCreationExpressionSyntax newAnon)
                {
                    reportDiagnostic(Diagnostic.Create(anonCreationRule, newAnon.NewKeyword.GetLocation(), EmptyMessageArgs));
                    return;
                }
            }

            if (rules.TryGet(AllocationRules.NewArrayRule.Id, out var newArrayRule))
            {
                if (node is ArrayCreationExpressionSyntax newArr)
                {
                    reportDiagnostic(Diagnostic.Create(newArrayRule, newArr.NewKeyword.GetLocation(), EmptyMessageArgs));
                    return;
                }
            }

            if (rules.TryGet(AllocationRules.NewObjectRule.Id, out var newObjectRule))
            {
                if (node is ObjectCreationExpressionSyntax newObj)
                {
                    var typeInfo = semanticModel.GetTypeInfo(newObj, cancellationToken);
                    if (typeInfo.ConvertedType != null && typeInfo.ConvertedType.TypeKind != TypeKind.Error && typeInfo.ConvertedType.IsReferenceType)
                    {
                        reportDiagnostic(Diagnostic.Create(newObjectRule, newObj.NewKeyword.GetLocation(), EmptyMessageArgs));
                    }
                    return;
                }
            }

            if (rules.TryGet(AllocationRules.LetCauseRule.Id, out var letClauseRule))
            {
                if (node is LetClauseSyntax letKind)
                {
                    reportDiagnostic(Diagnostic.Create(letClauseRule, letKind.LetKeyword.GetLocation(), EmptyMessageArgs));
                    return;
                }
            }
        }
    }
}