using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ILSpy.Backend.TreeProviders;

public class NamespaceNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public NamespaceNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata)
    {
        string namespaceName = string.IsNullOrEmpty(nodeMetadata.Name) ? "<global>" : nodeMetadata.Name;
        return new Dictionary<string, string>
        {
            [LanguageNames.CSharp] = $"namespace {namespaceName} {{ }}",
            [LanguageNames.IL] = $"namespace {namespaceName}",
        };
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath)
    {
        var decompiler = application.DecompilerBackend.GetDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return Enumerable.Empty<Node>();
        }

        var types = decompiler.TypeSystem.MainModule.TopLevelTypeDefinitions;
        var namespaces = new HashSet<string>(decompiler.TypeSystem.NameComparer);
        foreach (var type in types)
        {
            namespaces.Add(type.Namespace);
        }
        return namespaces
            .OrderBy(n => n)
            .Select(ns => new Node(
                new NodeMetadata(
                    AssemblyPath: assemblyPath,
                    Type: NodeType.Namespace,
                    Name: ns,
                    SymbolToken: 0,
                    ParentSymbolToken: 0),
                DisplayName: ns,
                Description: string.Empty,
                MayHaveChildren: true,
                SymbolModifiers: SymbolModifiers.None
            ));
    }

    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.Namespace)
        {
            return Enumerable.Empty<Node>();
        }

        return application.TreeNodeProviders.Type.CreateNodes(nodeMetadata.AssemblyPath, nodeMetadata.Name);
    }
}

