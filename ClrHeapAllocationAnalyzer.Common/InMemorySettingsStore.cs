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

using System.Collections.Generic;

namespace ClrHeapAllocationAnalyzer.Common
{
    public class InMemorySettingsStore : IWritableSettingsStore
    {
        private readonly IDictionary<string, bool> boolValues = new Dictionary<string, bool>();
        private readonly IDictionary<string, int> intValues = new Dictionary<string, int>();
        private readonly IDictionary<string, string> stringValues = new Dictionary<string, string>();

        public bool GetBoolean(string collectionPath, string propertyName, bool defaultValue)
        {
            return boolValues.ContainsKey(propertyName) ? boolValues[propertyName] : defaultValue;
        }

        public int GetInt32(string collectionPath, string propertyName, int defaultValue)
        {
            return intValues.ContainsKey(propertyName) ? intValues[propertyName] : defaultValue;
        }

        public string GetString(string collectionPath, string propertyName, string defaultValue)
        {
            return stringValues.ContainsKey(propertyName) ? stringValues[propertyName] : defaultValue;
        }

        public bool CollectionExists(string collectionPath)
        {
            return true;
        }

        public void SetBoolean(string collectionPath, string propertyName, bool value)
        {
            boolValues[propertyName] = value;
        }

        public void SetInt32(string collectionPath, string propertyName, int value)
        {
            intValues[propertyName] = value;
        }

        public void SetString(string collectionPath, string propertyName, string value)
        {
            stringValues[propertyName] = value;
        }

        public void CreateCollection(string collectionPath)
        {
        }
    }
}
