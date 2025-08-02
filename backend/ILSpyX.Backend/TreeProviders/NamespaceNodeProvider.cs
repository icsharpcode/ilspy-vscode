using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class NamespaceNodeProvider(TypeNodeProvider typeNodeProvider, DecompilerBackend decompilerBackend)
    : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        string namespaceName = string.IsNullOrEmpty(nodeMetadata.Name) ? "<global>" : nodeMetadata.Name;
        return outputLanguage switch
        {
            LanguageName.IL => DecompileResult.WithCode($"namespace {namespaceName}"),
            _ => DecompileResult.WithCode($"namespace {namespaceName} {{ }}")
        };
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return Task.FromResult(nodeMetadata?.Type != NodeType.Namespace
            ? []
            : typeNodeProvider.CreateNodes(nodeMetadata.AssemblyPath, nodeMetadata.Name));
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath)
    {
        var decompiler = decompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return [];
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
                    assemblyPath,
                    NodeType.Namespace,
                    ns,
                    0,
                    0,
                    true),
                ns,
                string.Empty,
                true
            ));
    }
}