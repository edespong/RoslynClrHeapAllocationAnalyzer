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

using System;
using System.Collections.Immutable;
using System.Linq;
using ClrHeapAllocationAnalyzer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClrHeapAllocationAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EnumeratorAllocationAnalyzer : AllocationAnalyzer
    {
        protected override SyntaxKind[] Expressions => new[] { SyntaxKind.ForEachStatement, SyntaxKind.InvocationExpression };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(AllocationRules.GetDescriptor(AllocationRules.ReferenceTypeEnumeratorRule.Id));

        private static readonly object[] EmptyMessageArgs = { };

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, EnabledRules rules)
        {
            if (!rules.TryGet(AllocationRules.ReferenceTypeEnumeratorRule.Id, out DiagnosticDescriptor rule))
            {
                return;
            }

            var node = context.Node;
            var semanticModel = context.SemanticModel;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            var cancellationToken = context.CancellationToken;
            string filePath = node.SyntaxTree.FilePath;
            if (node is ForEachStatementSyntax foreachExpression)
            {
                var typeInfo = semanticModel.GetTypeInfo(foreachExpression.Expression, cancellationToken);
                if (typeInfo.Type == null)
                    return;

                if (typeInfo.Type.Name == "String" && typeInfo.Type.ContainingNamespace.Name == "System")
                {
                    // Special case for System.String which is optmizined by
                    // the compiler and does not result in an allocation.
                    return;
                }

                // Regular way of getting the enumerator
                ImmutableArray<ISymbol> enumerator = typeInfo.Type.GetMembers("GetEnumerator");
                if ((enumerator == null || enumerator.Length == 0) && typeInfo.ConvertedType != null)
                {
                    // 1st we try and fallback to using the ConvertedType
                    enumerator = typeInfo.ConvertedType.GetMembers("GetEnumerator");
                }
                if ((enumerator == null || enumerator.Length == 0) && typeInfo.Type.Interfaces != null)
                {
                    // 2nd fallback, now we try and find the IEnumerable Interface explicitly
                    var iEnumerable = typeInfo.Type.Interfaces.Where(i => i.Name == "IEnumerable").ToImmutableArray();
                    if (iEnumerable != null && iEnumerable.Length > 0)
                    {
                        enumerator = iEnumerable[0].GetMembers("GetEnumerator");
                    }
                }

                if (enumerator != null && enumerator.Length > 0)
                {
                    if (enumerator[0] is IMethodSymbol methodSymbol) // probably should do something better here, hack.
                    {
                        if (methodSymbol.ReturnType.IsReferenceType && methodSymbol.ReturnType.SpecialType != SpecialType.System_Collections_IEnumerator)
                        {
                            reportDiagnostic(Diagnostic.Create(rule, foreachExpression.InKeyword.GetLocation(), EmptyMessageArgs));
                        }
                    }
                }

                return;
            }

            if (node is InvocationExpressionSyntax invocationExpression)
            {
                var methodInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol as IMethodSymbol;
                if (methodInfo?.ReturnType != null && methodInfo.ReturnType.IsReferenceType)
                {
                    if (methodInfo.ReturnType.AllInterfaces != null)
                    {
                        foreach (var @interface in methodInfo.ReturnType.AllInterfaces)
                        {
                            if (@interface.SpecialType == SpecialType.System_Collections_Generic_IEnumerator_T || @interface.SpecialType == SpecialType.System_Collections_IEnumerator)
                            {
                                reportDiagnostic(Diagnostic.Create(rule, invocationExpression.GetLocation(), EmptyMessageArgs));
                            }
                        }
                    }
                }
            }
        }
    }
}
