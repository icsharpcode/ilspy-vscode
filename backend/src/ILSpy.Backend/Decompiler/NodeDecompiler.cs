using ILSpy.Backend.Model;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILSpy.Backend.Decompiler
{
    public class NodeDecompiler
    {
        private readonly IDecompilerBackend decompilerBackend;

        public NodeDecompiler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public IDictionary<string, string>? GetCode(Node node)
        {
            return node.Type switch
            {
                NodeType.Assembly => decompilerBackend.GetCode(node.AssemblyPath, EntityHandle.AssemblyDefinition),
                NodeType.Namespace => GetNamespaceCode(node.Name),
                NodeType.Class or NodeType.Enum or NodeType.Delegate or NodeType.Interface or NodeType.Struct =>
                    decompilerBackend.GetCode(node.AssemblyPath, MetadataTokens.EntityHandle(node.SymbolToken)),
                NodeType.Method or NodeType.Property or NodeType.Event or NodeType.Field =>
                    decompilerBackend.GetCode(node.AssemblyPath, MetadataTokens.EntityHandle(node.SymbolToken)),
                NodeType.ReferencesRoot => GetReferencesRootCode(node),
                NodeType.AssemblyReference => GetReferenceCode(node),
                _ => null
            };
        }

        private static IDictionary<string, string> GetNamespaceCode(string @namespace)
        {
            string namespaceName = string.IsNullOrEmpty(@namespace) ? "<global>" : @namespace;
            return new Dictionary<string, string>
            {
                [LanguageNames.CSharp] = $"namespace {namespaceName} {{ }}",
                [LanguageNames.IL] = $"namespace {namespaceName}",
            };
        }

        private IDictionary<string, string> GetReferencesRootCode(Node node)
        {
            var references = decompilerBackend.ListAssemblyReferences(node.AssemblyPath);
            var code = string.Join('\n', references.Select(reference => $"// {reference}"));

            return new Dictionary<string, string>
            {
                [LanguageNames.CSharp] = code,
                [LanguageNames.IL] = code,
            };
        }

        private IDictionary<string, string> GetReferenceCode(Node node)
        {
            var code = $"// {node.Name}";
            return new Dictionary<string, string>
            {
                [LanguageNames.CSharp] = code,
                [LanguageNames.IL] = code,
            };
        }
    }
}

