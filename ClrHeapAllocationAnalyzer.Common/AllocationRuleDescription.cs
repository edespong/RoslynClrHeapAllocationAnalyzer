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

using Microsoft.CodeAnalysis;

namespace ClrHeapAllocationAnalyzer.Common {
    public struct AllocationRuleDescription {
        public string Id { get; }
        public string Title { get; }
        public string MessageFormat { get; }
        public DiagnosticSeverity Severity { get; }
        public string HelpLinkUri { get; }

        public AllocationRuleDescription(string id, string title, string messageFormat, DiagnosticSeverity severity) {
            Id = id;
            Title = title;
            MessageFormat = messageFormat;
            Severity = severity;
            HelpLinkUri = null;
        }

        public AllocationRuleDescription(string id, string title, string messageFormat, DiagnosticSeverity severity, string helpLinkUri) {
            Id = id;
            Title = title;
            MessageFormat = messageFormat;
            Severity = severity;
            HelpLinkUri = helpLinkUri;
        }

        public AllocationRuleDescription WithSeverity(DiagnosticSeverity severity) {
            return new AllocationRuleDescription(Id, Title, MessageFormat, severity);
        }
    }
}
