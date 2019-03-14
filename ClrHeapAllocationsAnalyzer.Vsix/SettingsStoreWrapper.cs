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
using Microsoft.VisualStudio.Settings;

namespace ClrHeapAllocationAnalyzer.Vsix
{
    internal class SettingsStoreWrapper : IWritableSettingsStore
    {
        private readonly WritableSettingsStore settingsStore;

        public SettingsStoreWrapper(WritableSettingsStore store)
        {
            settingsStore = store;
        }

        public bool GetBoolean(string collectionPath, string propertyName, bool defaultValue)
        {
            return settingsStore.GetBoolean(collectionPath, propertyName, defaultValue);
        }

        public int GetInt32(string collectionPath, string propertyName, int defaultValue)
        {
            return settingsStore.GetInt32(collectionPath, propertyName, defaultValue);
        }

        public bool CollectionExists(string collectionPath)
        {
            return settingsStore.CollectionExists(collectionPath);
        }

        public void SetBoolean(string collectionPath, string propertyName, bool value)
        {
            settingsStore.SetBoolean(collectionPath, propertyName, value);
        }

        public void SetInt32(string collectionPath, string propertyName, int value)
        {
            settingsStore.SetInt32(collectionPath, propertyName, value);
        }

        public void CreateCollection(string collectionPath)
        {
            settingsStore.CreateCollection(collectionPath);
        }
    }
}
