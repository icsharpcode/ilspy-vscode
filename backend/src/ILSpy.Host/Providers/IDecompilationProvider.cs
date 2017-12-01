﻿// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;

namespace ILSpy.Host.Providers
{
    public interface IDecompilationProvider
    {
        bool AddAssembly(string path);
        IEnumerable<string> ListNamespaces(string assemblyPath);
        IEnumerable<MemberData> ListTypes(string assemblyPath, string @namespace);
        string GetMemberCode(string assemblyPath, MetadataToken memberToken);
        string GetCode(string assemblyPath, TokenType tokenType, uint rid);
        IEnumerable<MemberData> GetChildren(string assemblyPath, TokenType tokenType, uint rid);
    }
}