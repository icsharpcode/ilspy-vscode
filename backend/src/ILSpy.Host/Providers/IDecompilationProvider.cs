// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata;

namespace ILSpy.Host.Providers
{
    public interface IDecompilationProvider
    {
        bool AddAssembly(string path);
        IEnumerable<string> ListNamespaces(string assemblyPath);
        IEnumerable<MemberData> ListTypes(string assemblyPath, string @namespace);
        string GetCode(string assemblyPath, EntityHandle handle);
        IEnumerable<MemberData> GetMembers(string assemblyPath, TypeDefinitionHandle handle);
    }
}