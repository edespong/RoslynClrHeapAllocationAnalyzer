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
using Microsoft.CodeAnalysis;

namespace ClrHeapAllocationAnalyzer.Test
{
    internal class DiagnosticEqualityComparer : IEqualityComparer<Diagnostic>
    {
        public static DiagnosticEqualityComparer Instance = new DiagnosticEqualityComparer();

        public bool Equals(Diagnostic x, Diagnostic y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Diagnostic obj)
        {
            return Combine(obj?.Descriptor.GetHashCode(),
                        Combine(obj?.GetMessage().GetHashCode(),
                         Combine(obj?.Location.GetHashCode(),
                          Combine(obj?.Severity.GetHashCode(), obj?.WarningLevel)
                        )));
        }

        internal static int Combine(int? newKeyPart, int? currentKey)
        {
            int hash = unchecked(currentKey.Value * (int)0xA5555529);

            if (newKeyPart.HasValue)
            {
                return unchecked(hash + newKeyPart.Value);
            }

            return hash;
        }
    }
}