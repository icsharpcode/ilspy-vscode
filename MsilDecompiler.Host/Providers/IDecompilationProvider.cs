using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MsilDecompiler.Host.Providers
{
    public interface IDecompilationProvider
    {
        bool AddAssembly(string path);
        IEnumerable<Tuple<string, MetadataToken>> GetTypeTuples(string assemblyPath);
        string GetMemberCode(string assemblyPath, MetadataToken memberToken);
        string GetCode(string assemblyPath, TokenType tokenType, uint rid);
        IEnumerable<Tuple<string, MetadataToken>> GetChildren(string assemblyPath, TokenType tokenType, uint rid);
    }
}