// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Backend.Decompiler;

using ILSpy.Backend.Model;
using System.Collections.Generic;
using System.Reflection.Metadata;

public interface IDecompilerBackend
{
    AssemblyData? AddAssembly(string? path);
    bool RemoveAssembly(string? path);
    IEnumerable<string> ListAssemblyReferences(string? assemblyPath);
    IEnumerable<string> ListNamespaces(string? assemblyPath);
    IEnumerable<MemberData> ListTypes(string? assemblyPath, string? @namespace);
    IDictionary<string, string> GetCode(string? assemblyPath, EntityHandle handle);
    IEnumerable<MemberData> GetMembers(string? assemblyPath, TypeDefinitionHandle handle);
}
