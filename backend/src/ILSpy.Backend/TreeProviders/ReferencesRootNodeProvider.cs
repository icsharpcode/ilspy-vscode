using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ILSpy.Backend.TreeProviders;

public class ReferencesRootNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public ReferencesRootNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata)
    {
        var code = string.Join('\n',
            GetAssemblyReferences(nodeMetadata.AssemblyPath)
                .Select(reference => $"// {reference}"));

        return new Dictionary<string, string>
        {
            [LanguageNames.CSharp] = code,
            [LanguageNames.IL] = code,
        };
    }

    private IEnumerable<string> GetAssemblyReferences(string assemblyPath)
    {
        var decompiler = application.DecompilerBackend.GetDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return Enumerable.Empty<string>();
        }

        HashSet<string> references = new(decompiler.TypeSystem.NameComparer);
        foreach (var ar in decompiler.TypeSystem.MainModule.PEFile.AssemblyReferences)
        {
            references.Add(ar.FullName);
        }
        return references.OrderBy(n => n);
    }

    public Node CreateNode(string assemblyPath)
    {
        return new Node(
            new NodeMetadata(
                AssemblyPath: assemblyPath,
                Type: NodeType.ReferencesRoot,
                Name: "References",
                SymbolToken: 0,
                ParentSymbolToken: 0),
            DisplayName: "References",
            Description: string.Empty,
            MayHaveChildren: true,
            SymbolModifiers: SymbolModifiers.None
        );
    }

    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.ReferencesRoot)
        {
            return Enumerable.Empty<Node>();
        }

        return application.TreeNodeProviders.AssemblyReference.CreateNodes(nodeMetadata.AssemblyPath);
    }
}

