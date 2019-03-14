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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace ClrHeapAllocationAnalyzer
{
    public abstract class AllocationAnalyzer : DiagnosticAnalyzer
    {
        protected abstract SyntaxKind[] Expressions { get; }

        protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context, EnabledRules rules);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, Expressions);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (AllocationRules.IsIgnoredFile(context.Node.SyntaxTree.FilePath))
            {
                return;
            }

            if (HasIgnoreAttribute(context))
            {
                return;
            }

            var ids = SupportedDiagnostics.Select(x => x.Id).ToArray();
            EnabledRules rules = AllocationRules.GetEnabledRules(ids);
            if (!rules.AnyEnabled)
            {
                return;
            }
                          
            rules = HotPathAnalysis.GetEnabledRules(rules.All(), context);
            if (!rules.AnyEnabled)
            {
                return;
            }

            AnalyzeNode(context, rules);
        }

        private static bool HasIgnoreAttribute(SyntaxNodeAnalysisContext context)
        {
            return context.ContainingSymbol.GetAttributes().Any(AllocationRules.IsIgnoredAttribute) ||
                context.ContainingSymbol.ContainingType.GetAttributes().Any(AllocationRules.IsIgnoredAttribute);
        }
    }
}
