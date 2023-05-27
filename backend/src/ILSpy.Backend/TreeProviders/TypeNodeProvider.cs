using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace ILSpy.Backend.TreeProviders;

public class TypeNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public TypeNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata)
    {
        return application.DecompilerBackend.GetCode(
            nodeMetadata.AssemblyPath,
            MetadataTokens.EntityHandle(nodeMetadata.SymbolToken));
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath, string @namespace)
    {
        var decompiler = application.DecompilerBackend.GetDecompiler(assemblyPath);
        if (decompiler is null)
        {
            yield break;
        }

        var currentNamespace = decompiler.TypeSystem.MainModule.RootNamespace;
        string[] parts = @namespace.Split('.');

        if (!(parts.Length == 1 && string.IsNullOrEmpty(parts[0])))
        {
            foreach (var part in parts)
            {
                var nested = currentNamespace.GetChildNamespace(part);
                if (nested == null)
                    yield break;
                currentNamespace = nested;
            }
        }

        foreach (var t in currentNamespace.Types.OrderBy(t => t.FullName))
        {
            var name = t.TypeToString(includeNamespace: false);
            yield return new Node(
                    new NodeMetadata(
                        AssemblyPath: assemblyPath,
                        Type: NodeTypeHelper.GetNodeTypeFromTypeKind(t.Kind),
                        Name: name,
                        SymbolToken: MetadataTokens.GetToken(t.MetadataToken),
                        ParentSymbolToken: 0),
                    DisplayName: name,
                    Description: "",
                    MayHaveChildren: true,
                    SymbolModifiers: NodeTypeHelper.GetSymbolModifiers(t)
                );
        }
    }

    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null || !NodeTypeHelper.IsTypeNode(nodeMetadata.Type))
        {
            return Enumerable.Empty<Node>();
        }

        return application.TreeNodeProviders.Member.CreateNodes(
            nodeMetadata.AssemblyPath, nodeMetadata.SymbolToken);
    }
}
