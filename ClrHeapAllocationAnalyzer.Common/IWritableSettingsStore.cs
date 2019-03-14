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

namespace ClrHeapAllocationAnalyzer.Common
{
    /// <summary>
    /// Exposes methods for getting and setting settings values.
    ///
    /// Analogous to the Microsoft.VisualStudio.Settings.WritableSettingsStore,
    /// but with fewer exposed methods.
    /// </summary>
    /// <remarks>
    /// The reason this interface exists is that we do not want to reference
    /// the Visual Studio specific WritableSettingsStore to this project.
    /// </remarks>
    public interface IWritableSettingsStore
    {
        bool GetBoolean(string collectionPath, string propertyName, bool defaultValue);
        int GetInt32(string collectionPath, string propertyName, int defaultValue);
        string GetString(string collectionPath, string propertyName, string defaultValue);
        bool CollectionExists(string collectionPath);
        void SetBoolean(string collectionPath, string propertyName, bool value);
        void SetInt32(string collectionPath, string propertyName, int value);
        void SetString(string collectionPath, string propertyName, string value);
        void CreateCollection(string collectionPath);
    }
}
