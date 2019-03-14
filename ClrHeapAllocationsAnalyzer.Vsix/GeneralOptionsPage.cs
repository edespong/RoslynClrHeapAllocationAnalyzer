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

using System.ComponentModel;
using ClrHeapAllocationAnalyzer.Common;
using Microsoft.VisualStudio.Shell;

namespace ClrHeapAllocationAnalyzer.Vsix {
    public class GeneralOptionsPage : DialogPage
    {
        [Category("Heap Allocation Analyzer")]
        [DisplayName("Enabled")]
        [Description("Determines whether to run any analysis or not. Note that the extension is still loaded and executed. Use the 'Extension and Updates' to disable or remove it permanentely.")]
        public bool Enabled { get; set; }
        
        [Category("Heap Allocation Analyzer")]
        [DisplayName("Ignored Attributes")]
        [Description("Methods with any of the given attributes will not be analyzed. Format: A comma-separated list of fully qualified names.")]
        public string IgnoredAttributes { get; set; }

        [Category("Heap Allocation Analyzer")]
        [DisplayName("Ignored Files Patterns")]
        [Description("Files matching any of the given patterns will not be analyzed. Format: A comma-separated list of patterns. Can include wildcard characters.")]
        public string IgnoredFilesPatterns { get; set; }

        [Category("Heap Allocation Analyzer")]
        [DisplayName("Hot Path Analysis")]
        [Description("If set to True, only methods or classes marked with an attribute defined below will be analyzed. If False, everything will be analyzed in a class if there is no attribute defined. If False and at least one attribute is defined in a class, only that member will be analyzed. Note that you must add at least one attribute below if turning this setting to True.")]
        public bool OnlyReportOnHotPath { get; set; }

        [Category("Heap Allocation Analyzer")]
        [DisplayName("Hot Path Attributes")]
        [Description("Classes and method with any of the given attributes are considered performance critical and analyzed according to the setting 'Analyze Hot Paths'. Format: A comma-separated list of fully qualified names.")]
        public string HotPathAttributes { get; set; }
        
        public GeneralOptionsPage()
        {
            Enabled = AllocationRules.Settings.Enabled;
            IgnoredAttributes = AllocationRules.Settings.IgnoredAttributes;
            IgnoredFilesPatterns = AllocationRules.Settings.IgnoredFilesPatterns;
            OnlyReportOnHotPath = AllocationRules.Settings.OnlyReportOnHotPath;
            HotPathAttributes = AllocationRules.Settings.HotPathAttributes;
        }

        protected override void OnApply(PageApplyEventArgs args)
        {
            base.OnApply(args);
            AllocationRules.Settings.Enabled = Enabled;
            AllocationRules.Settings.IgnoredAttributes = IgnoredAttributes;
            AllocationRules.Settings.IgnoredFilesPatterns = IgnoredFilesPatterns;
            AllocationRules.Settings.OnlyReportOnHotPath = OnlyReportOnHotPath;
            AllocationRules.Settings.HotPathAttributes = HotPathAttributes;
        }
    }
}