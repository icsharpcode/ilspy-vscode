// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace ILSpy.Host.Providers
{
    public interface IDecompilationProvider
    {
        bool AddAssembly(string path);
        IEnumerable<string> ListNamespaces(string assemblyPath);
        IEnumerable<MemberData> ListTypes(string assemblyPath);
        string GetMemberCode(string assemblyPath, MetadataToken memberToken);
        string GetCode(string assemblyPath, TokenType tokenType, uint rid);
        IEnumerable<MemberData> GetChildren(string assemblyPath, TokenType tokenType, uint rid);
    }
}