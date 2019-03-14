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
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ClrHeapAllocationAnalyzer
{
    internal class HotPathAnalysis
    {
        public static EnabledRules GetEnabledRules(ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
          SyntaxNodeAnalysisContext context)
        {
            bool onlyHotPaths = AllocationRules.Settings.OnlyReportOnHotPath;
            if (!ShouldAnalyze(AllocationRules.HotPathAttributes.Any(), onlyHotPaths, context))
            {
                return EnabledRules.None;
            }

            var allDiagnostics = supportedDiagnostics.ToDictionary(x => x.Id, x => x);
            return new EnabledRules(allDiagnostics);
        }

        private static bool IsHotPathAttribute(AttributeData attribute)
        {
            return AllocationRules.HotPathAttributes.Contains(
                (attribute.AttributeClass.ContainingNamespace.ToString(), attribute.AttributeClass.Name));
        }

        /// <summary>
        /// Based on the hot path settings and the current context, should an
        /// analysis be performed?
        /// </summary>
        private static bool ShouldAnalyze(bool isHotPathAttributesDefined, bool onlyReportOnHotPath,
            SyntaxNodeAnalysisContext context)
        {
            if (onlyReportOnHotPath && !isHotPathAttributesDefined)
            {
                // Inform the user that there might be an issue with the
                // configuration.
                var descriptor = new DiagnosticDescriptor("HAA0000", "Settings issue",
                    "The heap allocation analyzer settings are set to only report on hot paths, " +
                    "but no attribute has been defined.", "Performance", DiagnosticSeverity.Info, true);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
                return false;
            }

            if (!isHotPathAttributesDefined)
            {
                // No attribute defined in settings, report on everything.
                return true;
            }

            ImmutableArray<AttributeData> symbolAttributes =
                context.ContainingSymbol.GetAttributes();
            if (symbolAttributes.Any(IsHotPathAttribute))
            {
                // Always perform analysis regardless of setting when a hot path
                // is found.
                return true;
            }

            ImmutableArray<AttributeData> typeAttributes = context.ContainingSymbol.ContainingType.GetAttributes();
            if (typeAttributes.Any(IsHotPathAttribute))
            {
                // Always perform analysis regardless of setting when a hot path
                // is found.
                return true;
            }

            if (onlyReportOnHotPath)
            {
                // There was no hot path specified.
                return false;
            }

            if (context.ContainingSymbol.ContainingType == null)
            {
                // Happens for scripts and snippets.
                return true;
            }

            // Check for other members in the type for hot paths. If there is
            // one, lets not do any analysis for the current context.
            foreach (ISymbol member in context.ContainingSymbol.ContainingType.GetMembers())
            {
                if (ReferenceEquals(context.ContainingSymbol, member))
                {
                    // Already checked above.
                    continue;
                }

                if (member.GetAttributes().Any(IsHotPathAttribute))
                {
                    return false;
                }
            }

            // The containing type does not have any other member with a hot
            // path attribute -> do analysis.
            return true;
        }
    }
}
