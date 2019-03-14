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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ClrHeapAllocationAnalyzer.Common
{
    public class EnabledRules
    {
        private readonly IReadOnlyDictionary<string, DiagnosticDescriptor> rules;

        public EnabledRules(IReadOnlyDictionary<string, DiagnosticDescriptor> rules)
        {
            this.rules = rules;
        }

        public static EnabledRules None = new EnabledRules(new Dictionary<string, DiagnosticDescriptor>());

        public bool AnyEnabled => rules.Count > 0;

        public bool IsEnabled(string ruleId)
        {
            return rules.ContainsKey(ruleId);
        }

        public ImmutableArray<DiagnosticDescriptor> All()
        {
            return ImmutableArray.ToImmutableArray(this.rules.Values);
        }

        public DiagnosticDescriptor Get(string ruleId)
        {
            if (!rules.ContainsKey(ruleId))
            {
                throw new ArgumentException($"Rule '{ruleId}' is not among the enabled rules.", nameof(ruleId));
            }

            return rules[ruleId];
        }

        public bool TryGet(string ruleId, out DiagnosticDescriptor descriptor)
        {
            return rules.TryGetValue(ruleId, out descriptor);
        }
    }
}
