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
using Microsoft.CodeAnalysis;

namespace ClrHeapAllocationAnalyzer.Common
{
    public interface IHeapAllocationAnalyzerSettings
    {
        event EventHandler SettingsChanged;

        bool Enabled { get; set; }

        DiagnosticSeverity GetSeverity(string ruleId, DiagnosticSeverity defaultValue);

        DiagnosticSeverity GetSeverity(AllocationRuleDescription defaultDescription);

        void SetSeverity(string ruleId, DiagnosticSeverity severity);
    }
}
