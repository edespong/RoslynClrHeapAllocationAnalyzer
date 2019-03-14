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
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ClrHeapAllocationAnalyzer.Common
{
    public static partial class AllocationRules
    {
        private static IHeapAllocationAnalyzerSettings defaultSettings;

        private static IHeapAllocationAnalyzerSettings settings;

        private static readonly HashSet<ValueTuple<string, string>> IgnoredAttributes = new HashSet<(string, string)>();

        private static readonly IList<string> IgnoredFilesPatterns = new List<string>();

        private static readonly Dictionary<string, AllocationRuleDescription> Descriptions =
            new Dictionary<string, AllocationRuleDescription>();

        static AllocationRules()
        {
            foreach (AllocationRuleDescription rule in DefaultValues())
            {
                Descriptions.Add(rule.Id, rule);
            }

            HotPathAttributes = new HashSet<(string, string)>();
        }

        public static IHeapAllocationAnalyzerSettings Settings
        {
            get
            {
                if (settings != null)
                {
                    return settings;
                }

                if (defaultSettings == null)
                {

                    defaultSettings = new HeapAllocationAnalyzerSettings(new InMemorySettingsStore(), DefaultValues());
                }

                return defaultSettings;
            }
            set
            {
                if (settings != null)
                {
                    settings.SettingsChanged -= OnSettingsChanged;
                }

                settings = value;

                // We now have access to settings. Load the severities for the
                // rules.
                LoadSeverities();

                if (settings != null)
                {
                    settings.SettingsChanged += OnSettingsChanged;
                    LoadSettings();
                }
            }
        }

        public static HashSet<ValueTuple<string, string>> HotPathAttributes { get; private set; }

        public static bool IsIgnoredFile(string filePath)
        {
            return IgnoredFilesPatterns.Any(pattern =>
                Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString(filePath, pattern, Microsoft.VisualBasic.CompareMethod.Text));
        }

        public static bool IsIgnoredAttribute(AttributeData attribute)
        {
            return IgnoredAttributes.Contains(
                (attribute.AttributeClass.ContainingNamespace.ToString(), attribute.AttributeClass.Name));
        }

        public static DiagnosticDescriptor GetDescriptor(string ruleId)
        {
            if (!Descriptions.ContainsKey(ruleId))
            {
                throw new ArgumentException($"Cannot find description for rule {ruleId}", nameof(ruleId));
            }

            AllocationRuleDescription d = Descriptions[ruleId];
            DiagnosticSeverity severity = d.Severity;
            severity = Settings.GetSeverity(d);

            bool isEnabled = severity != DiagnosticSeverity.Hidden;
            return new DiagnosticDescriptor(d.Id, d.Title, d.MessageFormat, "Performance", severity, isEnabled, helpLinkUri: d.HelpLinkUri);
        }

        public static IEnumerable<AllocationRuleDescription> GetDescriptions()
        {
            return Descriptions.Values;
        }

        public static EnabledRules GetEnabledRules(IEnumerable<string> ruleIds)
        {
            if (!Settings.Enabled)
            {
                return EnabledRules.None;
            }

            Dictionary<string, DiagnosticDescriptor> result = null;
            foreach (var ruleId in ruleIds)
            {
                if (IsEnabled(ruleId))
                {
                    if (result == null)
                    {
                        result = new Dictionary<string, DiagnosticDescriptor>();
                    }

                    result.Add(ruleId, GetDescriptor(ruleId));
                }
            }

            return result != null ? new EnabledRules(result) : EnabledRules.None;
        }

        public static bool IsEnabled(string ruleId)
        {
            if (!Descriptions.ContainsKey(ruleId))
            {
                throw new ArgumentException($"Cannot find description for rule {ruleId}", nameof(ruleId));
            }

            return Settings.Enabled && Descriptions[ruleId].Severity != DiagnosticSeverity.Hidden;
        }

        private static void OnSettingsChanged(object sender, EventArgs eventArgs)
        {
            LoadSettings();
        }

        private static void LoadSettings()
        {
            LoadSeverities();
            LoadIgnoredFilesPatterns(Settings.IgnoredFilesPatterns);
            LoadIgnoredAttributes(Settings.IgnoredAttributes);
            LoadHotPathAttributes(Settings.HotPathAttributes);
        }

        private static void LoadSeverities()
        {
            var descriptionsCopy = new Dictionary<string, AllocationRuleDescription>(Descriptions);
            foreach (var d in descriptionsCopy)
            {
                DiagnosticSeverity severity = Settings.GetSeverity(d.Key, d.Value.Severity);
                if (Descriptions[d.Key].Severity != severity)
                {
                    Descriptions[d.Key] = Descriptions[d.Key].WithSeverity(severity);
                }
            }
        }

        /// <summary>
        /// Loads attributes for which, if applied to a method, will not cause
        /// analysis.
        /// </summary>
        /// <param name="ignoredAttributesStr">
        /// E.g. "System.CompilerServices.CompilerGeneratedAttribute, System.GeneratedCodeAttribute"
        /// </param>
        private static void LoadIgnoredAttributes(string ignoredAttributesStr)
        {
            LoadAttributes(ignoredAttributesStr, IgnoredAttributes);
        }

        /// <summary>
        /// Loads attributes that describes that the given code is performance
        /// critical.
        /// </summary>
        /// <param name="hotPathAttributesStr">
        /// E.g. "System.Performance.HotPath, MyCompany.Application.PerformanceCritical"
        /// </param>
        private static void LoadHotPathAttributes(string hotPathAttributesStr)
        {
            LoadAttributes(hotPathAttributesStr, HotPathAttributes);
        }

        /// <summary>
        /// Extracts attribute namespace and class name from a comma separated
        /// string and puts it into the given hashset.
        /// analysis.
        /// </summary>
        /// <param name="attributesString">
        /// E.g. "System.CompilerServices.CompilerGeneratedAttribute, System.GeneratedCodeAttribute"
        /// </param>
        private static void LoadAttributes(string attributesString, HashSet<ValueTuple<string, string>> attributeClasses)
        {
            attributeClasses.Clear();

            // E.g. { "System.CompilerServices.CompilerGeneratedAttribute", "System.GeneratedCodeAttribute" }
            string[] attributeStrings = attributesString?.Split(new[] { ',' }) ?? new string[] { };
            foreach (var attributeName in attributeStrings)
            {
                int index = attributeName.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                if (index == -1)
                {
                    continue;
                }

                // E.g. "System.CompilerServices"
                string @namespace = attributeName.Substring(0, index).Trim();
                // E.g. "CompilerGeneratedAttribute"
                string @class = attributeName.Substring(index + 1).Trim();

                attributeClasses.Add((@namespace, @class));
            }
        }

        private static void LoadIgnoredFilesPatterns(string ignoredFilesPatternsStr)
        {
            IgnoredFilesPatterns.Clear();
            string[] patterns = ignoredFilesPatternsStr?.Split(',') ?? new string[] { };
            foreach (var pattern in patterns)
            {
                IgnoredFilesPatterns.Add(pattern.Trim());
            }
        }
    }
}
