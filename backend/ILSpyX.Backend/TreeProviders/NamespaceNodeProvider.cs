using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class NamespaceNodeProvider(TypeNodeProvider typeNodeProvider, DecompilerBackend decompilerBackend)
    : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        string namespaceName = string.IsNullOrEmpty(nodeMetadata.Name) ? "<global>" : nodeMetadata.Name;
        return Task.FromResult(outputLanguage switch
        {
            LanguageName.IL => DecompileResult.WithCode($"namespace {namespaceName}"),
            _ => DecompileResult.WithCode($"namespace {namespaceName} {{ }}")
        });
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return nodeMetadata?.Type != NodeType.Namespace
            ? []
            : await typeNodeProvider.CreateNodes(nodeMetadata.GetAssemblyFileIdentifier(), nodeMetadata.Name);
    }

    public async Task<IEnumerable<Node>> CreateNodes(AssemblyFileIdentifier assemblyFile)
    {
        var decompiler = await decompilerBackend.CreateDecompiler(assemblyFile);
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
            .Select(ns => new Node
            {
                Metadata = new NodeMetadata
                {
                    AssemblyPath = assemblyFile.File,
                    BundledAssemblyName = assemblyFile.BundledAssemblyFile,
                    Type = NodeType.Namespace,
                    Name = ns,
                    IsDecompilable = true
                },
                DisplayName = ns,
                Description = string.Empty,
                MayHaveChildren = true,
            });
    }
}