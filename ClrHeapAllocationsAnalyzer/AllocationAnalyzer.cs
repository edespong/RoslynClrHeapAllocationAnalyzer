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

            if (context.ContainingSymbol.GetAttributes().Any(AllocationRules.IsIgnoredAttribute))
            {
                return;
            }

            // TODO(erik): Use supportedDiag directly instead?
            var ids = SupportedDiagnostics.Select(x => x.Id).ToArray();
            EnabledRules rules = AllocationRules.GetEnabledRules(ids);
            if (!rules.AnyEnabled)
            {
                return;
            }
                          
            rules = HotPathAnalysis.GetEnabledRules(rules.All(), context);

            AnalyzeNode(context, rules);
        }
    }
}
