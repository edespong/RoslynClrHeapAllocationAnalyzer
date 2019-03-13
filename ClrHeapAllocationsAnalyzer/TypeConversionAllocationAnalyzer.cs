namespace ClrHeapAllocationAnalyzer
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using ClrHeapAllocationAnalyzer.Common;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;


    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TypeConversionAllocationAnalyzer : AllocationAnalyzer
    {
        protected override SyntaxKind[] Expressions => new[] {
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.ReturnStatement,
            SyntaxKind.YieldReturnStatement,
            SyntaxKind.CastExpression,
            SyntaxKind.AsExpression,
            SyntaxKind.CoalesceExpression,
            SyntaxKind.ConditionalExpression,
            SyntaxKind.ForEachStatement,
            SyntaxKind.EqualsValueClause,
            SyntaxKind.Argument,
            SyntaxKind.ArrowExpressionClause,
            SyntaxKind.Interpolation
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                AllocationRules.GetDescriptor(AllocationRules.ValueTypeToReferenceTypeConversionRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.DelegateOnStructInstanceRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.MethodGroupAllocationRule.Id),
                AllocationRules.GetDescriptor(AllocationRules.ReadonlyMethodGroupAllocationRule.Id)
            );

        private static readonly object[] EmptyMessageArgs = { };

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, EnabledRules rules)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            string filePath = node.SyntaxTree.FilePath;
            bool assignedToReadonlyFieldOrProperty = 
                (context.ContainingSymbol as IFieldSymbol)?.IsReadOnly == true ||
                (context.ContainingSymbol as IPropertySymbol)?.IsReadOnly == true;

            bool isValueTypeToReferenceRuleEnabled = rules.TryGet(AllocationRules.ValueTypeToReferenceTypeConversionRule.Id,
                out DiagnosticDescriptor valueTypeToReferenceRule);

            // this.fooObjCall(10);
            // new myobject(10);
            if (node is ArgumentSyntax)
            {
                ArgumentSyntaxCheck(rules, node, semanticModel, assignedToReadonlyFieldOrProperty, reportDiagnostic, filePath, cancellationToken);
            }

            // object foo { get { return 0; } }
            if (isValueTypeToReferenceRuleEnabled && node is ReturnStatementSyntax)
            {
                ReturnStatementExpressionCheck(valueTypeToReferenceRule, node, semanticModel, reportDiagnostic, filePath, cancellationToken);
                return;
            }

            // yield return 0
            if (isValueTypeToReferenceRuleEnabled && node is YieldStatementSyntax)
            {
                YieldReturnStatementExpressionCheck(valueTypeToReferenceRule, node, semanticModel, reportDiagnostic, filePath, cancellationToken);
                return;
            }

            // object a = x ?? 0;
            // var a = 10 as object;
            if (isValueTypeToReferenceRuleEnabled && node is BinaryExpressionSyntax) // TODO(erik): Should this really be here?????
            {
                BinaryExpressionCheck(rules, node, semanticModel, assignedToReadonlyFieldOrProperty, reportDiagnostic, filePath, cancellationToken);
                return;
            }

            // for (object i = 0;;)
            if (node is EqualsValueClauseSyntax)
            {
                EqualsValueClauseCheck(rules, node, semanticModel, assignedToReadonlyFieldOrProperty, reportDiagnostic, filePath, cancellationToken);
                return;
            }

            // object = true ? 0 : obj
            if (isValueTypeToReferenceRuleEnabled && node is ConditionalExpressionSyntax)
            {
                ConditionalExpressionCheck(valueTypeToReferenceRule, node, semanticModel, reportDiagnostic, filePath, cancellationToken);
                return;
            }

            // string a = $"{1}";
            if (isValueTypeToReferenceRuleEnabled && node is InterpolationSyntax) {
                InterpolationCheck(valueTypeToReferenceRule, node, semanticModel, reportDiagnostic, filePath, cancellationToken);
                return;
            }

            // var f = (object)
            if (isValueTypeToReferenceRuleEnabled && node is CastExpressionSyntax)
            {
                CastExpressionCheck(valueTypeToReferenceRule, node, semanticModel, reportDiagnostic, filePath, cancellationToken);
                return;
            }

            // object Foo => 1
            if (node is ArrowExpressionClauseSyntax)
            {
                ArrowExpressionCheck(rules, node, semanticModel, assignedToReadonlyFieldOrProperty, reportDiagnostic, filePath, cancellationToken);
                return;
            }
        }

        private static void ReturnStatementExpressionCheck(DiagnosticDescriptor typeConversionRule, SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var returnStatementExpression = node as ReturnStatementSyntax;
            if (returnStatementExpression.Expression != null)
            {
                var returnConversionInfo = semanticModel.GetConversion(returnStatementExpression.Expression, cancellationToken);
                CheckTypeConversion(typeConversionRule, returnConversionInfo, reportDiagnostic, returnStatementExpression.Expression.GetLocation(), filePath);
            }
        }

        private static void YieldReturnStatementExpressionCheck(DiagnosticDescriptor typeConversionRule, SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var yieldExpression = node as YieldStatementSyntax;
            if (yieldExpression.Expression != null)
            {
                var returnConversionInfo = semanticModel.GetConversion(yieldExpression.Expression, cancellationToken);
                CheckTypeConversion(typeConversionRule, returnConversionInfo, reportDiagnostic, yieldExpression.Expression.GetLocation(), filePath);
            }
        }

        private static void ArgumentSyntaxCheck(EnabledRules rules, SyntaxNode node, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var argument = node as ArgumentSyntax;
            if (argument.Expression != null)
            {
                if (rules.IsEnabled(AllocationRules.ValueTypeToReferenceTypeConversionRule.Id))
                {
                    var argumentTypeInfo = semanticModel.GetTypeInfo(argument.Expression, cancellationToken);
                    var argumentConversionInfo = semanticModel.GetConversion(argument.Expression, cancellationToken);
                    CheckTypeConversion(rules.Get(AllocationRules.ValueTypeToReferenceTypeConversionRule.Id), argumentConversionInfo, reportDiagnostic, argument.Expression.GetLocation(), filePath);
                    CheckDelegateCreation(rules, argument.Expression, argumentTypeInfo, semanticModel, isAssignmentToReadonly, reportDiagnostic, argument.Expression.GetLocation(), filePath, cancellationToken);
                }
            }
        }

        private static void BinaryExpressionCheck(EnabledRules rules, SyntaxNode node, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var binaryExpression = node as BinaryExpressionSyntax;

            bool conversionRuleEnabled = rules.TryGet(AllocationRules.ValueTypeToReferenceTypeConversionRule.Id, out DiagnosticDescriptor conversionRule);

            // as expression
            if (conversionRuleEnabled && binaryExpression.IsKind(SyntaxKind.AsExpression) && binaryExpression.Left != null && binaryExpression.Right != null)
            {
                var leftT = semanticModel.GetTypeInfo(binaryExpression.Left, cancellationToken);
                var rightT = semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken);

                if (leftT.Type?.IsValueType == true && rightT.Type?.IsReferenceType == true)
                {
                    reportDiagnostic(Diagnostic.Create(conversionRule, binaryExpression.Left.GetLocation(), EmptyMessageArgs));
                }

                return;
            }

            if (binaryExpression.Right != null)
            {
                if (conversionRuleEnabled)
                {
                    var assignmentExprConversionInfo = semanticModel.GetConversion(binaryExpression.Right, cancellationToken);
                    CheckTypeConversion(conversionRule, assignmentExprConversionInfo, reportDiagnostic, binaryExpression.Right.GetLocation(), filePath);
                }

                var assignmentExprTypeInfo = semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken);
                CheckDelegateCreation(rules, binaryExpression.Right, assignmentExprTypeInfo, semanticModel, isAssignmentToReadonly, reportDiagnostic, binaryExpression.Right.GetLocation(), filePath, cancellationToken);
                return;
            }
        }

        private static void InterpolationCheck(DiagnosticDescriptor rule, SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var interpolation = node as InterpolationSyntax;
            var typeInfo = semanticModel.GetTypeInfo(interpolation.Expression, cancellationToken);
            if (typeInfo.Type?.IsValueType == true) {
                reportDiagnostic(Diagnostic.Create(rule, interpolation.Expression.GetLocation(), EmptyMessageArgs));
            }
        }

        private static void CastExpressionCheck(DiagnosticDescriptor rule, SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var castExpression = node as CastExpressionSyntax;
            if (castExpression.Expression != null)
            {
                var castTypeInfo = semanticModel.GetTypeInfo(castExpression, cancellationToken);
                var expressionTypeInfo = semanticModel.GetTypeInfo(castExpression.Expression, cancellationToken);

                if (castTypeInfo.Type?.IsReferenceType == true && expressionTypeInfo.Type?.IsValueType == true)
                {
                    reportDiagnostic(Diagnostic.Create(rule, castExpression.Expression.GetLocation(), EmptyMessageArgs));
                }
            }
        }

        private static void ConditionalExpressionCheck(DiagnosticDescriptor rule, SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var conditionalExpression = node as ConditionalExpressionSyntax;

            var trueExp = conditionalExpression.WhenTrue;
            var falseExp = conditionalExpression.WhenFalse;

            if (trueExp != null)
            {
                CheckTypeConversion(rule, semanticModel.GetConversion(trueExp, cancellationToken), reportDiagnostic, trueExp.GetLocation(), filePath);
            }

            if (falseExp != null)
            {
                CheckTypeConversion(rule, semanticModel.GetConversion(falseExp, cancellationToken), reportDiagnostic, falseExp.GetLocation(), filePath);
            }
        }

        private static void EqualsValueClauseCheck(EnabledRules rules, SyntaxNode node, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var initializer = node as EqualsValueClauseSyntax;
            if (initializer.Value != null)
            {
                var typeInfo = semanticModel.GetTypeInfo(initializer.Value, cancellationToken);
                var conversionInfo = semanticModel.GetConversion(initializer.Value, cancellationToken);
                CheckTypeConversion(rules.Get(AllocationRules.ValueTypeToReferenceTypeConversionRule.Id), conversionInfo, reportDiagnostic, initializer.Value.GetLocation(), filePath);
                CheckDelegateCreation(rules, initializer.Value, typeInfo, semanticModel, isAssignmentToReadonly, reportDiagnostic, initializer.Value.GetLocation(), filePath, cancellationToken);
            }
        }


        private static void ArrowExpressionCheck(EnabledRules rules, SyntaxNode node, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, string filePath, CancellationToken cancellationToken)
        {
            var syntax = node as ArrowExpressionClauseSyntax;

            var typeInfo = semanticModel.GetTypeInfo(syntax.Expression, cancellationToken);
            var conversionInfo = semanticModel.GetConversion(syntax.Expression, cancellationToken);
            CheckTypeConversion(rules.Get(AllocationRules.ValueTypeToReferenceTypeConversionRule.Id), conversionInfo, reportDiagnostic, syntax.Expression.GetLocation(), filePath);
            CheckDelegateCreation(rules, syntax, typeInfo, semanticModel, false, reportDiagnostic,
                syntax.Expression.GetLocation(), filePath, cancellationToken);
        }

        private static void CheckTypeConversion(DiagnosticDescriptor rule, Conversion conversionInfo, Action<Diagnostic> reportDiagnostic, Location location, string filePath)
        {
            if (conversionInfo.IsBoxing)
            {
                reportDiagnostic(Diagnostic.Create(rule, location, EmptyMessageArgs));
            }
        }

        private static void CheckDelegateCreation(EnabledRules rules, SyntaxNode node, TypeInfo typeInfo, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, Location location, string filePath, CancellationToken cancellationToken)
        {
            // special case: method groups
            if (typeInfo.ConvertedType?.TypeKind == TypeKind.Delegate)
            {
                // new Action<Foo>(MethodGroup); should skip this one
                var insideObjectCreation = node?.Parent?.Parent?.Parent?.Kind() == SyntaxKind.ObjectCreationExpression;
                if (node is ParenthesizedLambdaExpressionSyntax || node is SimpleLambdaExpressionSyntax ||
                    node is AnonymousMethodExpressionSyntax || node is ObjectCreationExpressionSyntax ||
                    insideObjectCreation)
                {
                    // skip this, because it's intended.
                }
                else
                {
                    if (rules.IsEnabled(AllocationRules.MethodGroupAllocationRule.Id) && node.IsKind(SyntaxKind.IdentifierName))
                    {
                        if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol) {
                            reportDiagnostic(Diagnostic.Create(rules.Get(AllocationRules.MethodGroupAllocationRule.Id), location, EmptyMessageArgs));
                        }
                    }
                    else if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        var memberAccess = node as MemberAccessExpressionSyntax;
                        if (semanticModel.GetSymbolInfo(memberAccess.Name, cancellationToken).Symbol is IMethodSymbol)
                        {
                            if (isAssignmentToReadonly && rules.TryGet(AllocationRules.ReadonlyMethodGroupAllocationRule.Id, out var readonlyMethodGroupAllocationRule))
                            {
                                reportDiagnostic(Diagnostic.Create(readonlyMethodGroupAllocationRule, location, EmptyMessageArgs));
                            }
                            else if (rules.TryGet(AllocationRules.MethodGroupAllocationRule.Id, out var methodGroupAllocationRule))
                            {
                                reportDiagnostic(Diagnostic.Create(methodGroupAllocationRule, location, EmptyMessageArgs));
                            }
                        }
                    } 
                    else if (node is ArrowExpressionClauseSyntax)
                    {
                        if (rules.TryGet(AllocationRules.MethodGroupAllocationRule.Id, out var methodGroupAllocationRule))
                        {
                            var arrowClause = node as ArrowExpressionClauseSyntax;
                            if (semanticModel.GetSymbolInfo(arrowClause.Expression, cancellationToken).Symbol is IMethodSymbol)
                            {
                                reportDiagnostic(Diagnostic.Create(methodGroupAllocationRule, location, EmptyMessageArgs));
                            }
                        }
                    }
                }

                if (rules.TryGet(AllocationRules.DelegateOnStructInstanceRule.Id, out var delegateOnStructRule))
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
                    if (symbolInfo?.ContainingType?.IsValueType == true && !insideObjectCreation)
                    {
                        reportDiagnostic(Diagnostic.Create(delegateOnStructRule, location, EmptyMessageArgs));
                    }
                }
            }
        }
    }
}