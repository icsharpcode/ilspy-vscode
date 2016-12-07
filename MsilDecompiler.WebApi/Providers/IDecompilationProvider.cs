using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace MsilDecompiler.WebApi.Providers
{
    public interface IDecompilationProvider
    {
        IEnumerable<Tuple<string, MetadataToken>> GetTypeTuples();
        string GetMemberCode(MetadataToken token);
        string GetCode(TokenType type, uint rid);
        IEnumerable<Tuple<string, MetadataToken>> GetChildren(TokenType type, uint rid);
    }
}