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
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ClrHeapAllocationAnalyzer.Common
{
    public class HeapAllocationAnalyzerSettings : IHeapAllocationAnalyzerSettings
    {
        private const string CollectionPath = "HeapAllocationAnalyzer";

        private static readonly bool DefaultEnabled = true;
        private static readonly string DefaultIgnoredAttributes = "System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.CodeDom.Compiler.GeneratedCodeAttribute";
        private static readonly string DefaultIgnoredFilesPatterns = "*.g.cs";
        private static readonly bool DefaultOnlyReportOnHotPath = false;
        private static readonly string DefaultHotPathAttributes = "Microsoft.Diagnostics.PerformanceSensitiveAttribute";

        private readonly IWritableSettingsStore settingsStore;

        private bool enabled = true;

        private string ignoredAttributes;

        private string ignoredFilesPatterns;

        private bool onlyReportOnHotPath;

        private string hotPathAttributes;

        private readonly IDictionary<string, DiagnosticSeverity> ruleSeverities;

        public event EventHandler SettingsChanged;

        public HeapAllocationAnalyzerSettings(IWritableSettingsStore store, IEnumerable<AllocationRuleDescription> allRules = null)
        {
            settingsStore = store;
            ruleSeverities = allRules?.ToDictionary(x => x.Id, x => x.Severity) ?? new Dictionary<string, DiagnosticSeverity>();
            LoadSettings();
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    OnSettingsChanged();
                }
            }
        }

        public string IgnoredAttributes
        {
            get => ignoredAttributes;
            set
            {
                if (value != ignoredAttributes)
                {
                    ignoredAttributes = value;
                    OnSettingsChanged();
                }
            }
        }

        public string IgnoredFilesPatterns
        {
            get => ignoredFilesPatterns;
            set
            {
                if (value != ignoredFilesPatterns)
                {
                    ignoredFilesPatterns = value;
                    OnSettingsChanged();
                }
            }
        }

        public bool OnlyReportOnHotPath
        {
            get => onlyReportOnHotPath;
            set
            {
                if (value != onlyReportOnHotPath)
                {
                    onlyReportOnHotPath = value;
                    OnSettingsChanged();
                }
            }
        }

        public string HotPathAttributes
        {
            get => hotPathAttributes;
            set
            {
                if (value != hotPathAttributes)
                {
                    hotPathAttributes = value;
                    OnSettingsChanged();
                }
            }
        }

        public DiagnosticSeverity GetSeverity(string ruleId, DiagnosticSeverity defaultSeverity)
        {
            if (ruleSeverities.ContainsKey(ruleId))
            {
                return ruleSeverities[ruleId];
            }
            else
            {
                ruleSeverities.Add(ruleId, defaultSeverity);
                return defaultSeverity;
            }
        }

        public DiagnosticSeverity GetSeverity(AllocationRuleDescription defaultDescription)
        {
            return GetSeverity(defaultDescription.Id, defaultDescription.Severity);
        }

        public void SetSeverity(string ruleId, DiagnosticSeverity severity)
        {
            if (ruleSeverities.ContainsKey(ruleId))
            {
                if (ruleSeverities[ruleId] == severity)
                {
                    return;
                }
                else
                {
                    ruleSeverities[ruleId] = severity;
                }
            }
            else
            {
                ruleSeverities.Add(ruleId, severity);
            }

            OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            SaveSettings();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LoadSettings()
        {
            try
            {
                enabled = settingsStore.GetBoolean(CollectionPath, "Enabled", true);
                ignoredAttributes = settingsStore.GetString(CollectionPath, "IgnoredAttributes", DefaultIgnoredAttributes);
                ignoredFilesPatterns = settingsStore.GetString(CollectionPath, "IgnoredFilesPatterns", DefaultIgnoredFilesPatterns);
                onlyReportOnHotPath = settingsStore.GetBoolean(CollectionPath, "OnlyReportOnHotPath", DefaultOnlyReportOnHotPath);
                hotPathAttributes = settingsStore.GetString(CollectionPath, "HotPathAttributes", DefaultHotPathAttributes);

                var ruleSeveritiesCopy = new Dictionary<string, DiagnosticSeverity>(ruleSeverities);
                foreach (var rule in ruleSeveritiesCopy)
                {
                    int severity = settingsStore.GetInt32(CollectionPath, rule.Key, (int)rule.Value);
                    ruleSeverities[rule.Key] = (DiagnosticSeverity)severity;
                }
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (!settingsStore.CollectionExists(CollectionPath))
                {
                    settingsStore.CreateCollection(CollectionPath);
                }
                
                settingsStore.SetBoolean(CollectionPath, "Enabled", Enabled);
                settingsStore.SetString(CollectionPath, "IgnoredAttributes", IgnoredAttributes);
                settingsStore.SetString(CollectionPath, "IgnoredFilesPatterns", IgnoredFilesPatterns);
                settingsStore.SetBoolean(CollectionPath, "OnlyReportOnHotPath", OnlyReportOnHotPath);
                settingsStore.SetString(CollectionPath, "HotPathAttributes", HotPathAttributes);

                foreach (var rule in ruleSeverities)
                {
                    settingsStore.SetInt32(CollectionPath, rule.Key, (int)rule.Value);
                }
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
        }
    }
}
