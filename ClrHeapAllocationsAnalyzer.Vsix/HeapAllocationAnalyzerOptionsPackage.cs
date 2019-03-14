﻿/* -----------------------------------------------------------------------------
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ClrHeapAllocationAnalyzer.Common;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace ClrHeapAllocationAnalyzer.Vsix
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)] // Load the package when a solution is opened
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(GeneralOptionsPage), "Heap Allocation Analyzer", "General", 0, 0, true)]
    //[ProvideOptionPage(typeof(RulesOptionPage), "Heap Allocation Analyzer", "Rules", 0, 0, true)]
    public sealed class HeapAllocationAnalyzerOptionsPackage : Package
    {
        /// <summary>
        /// Unique identifier for this package.
        /// </summary>
        public const string PackageGuidString = "6420BAEC-F7F2-452C-8B92-3B190519F7B3";

        /// <summary>
        /// Initialization of the package; this method is called right after
        /// the package is sited.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            WritableSettingsStore settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            AllocationRules.Settings = new HeapAllocationAnalyzerSettings(new SettingsStoreWrapper(settingsStore), AllocationRules.DefaultValues());
        }
    }
}
