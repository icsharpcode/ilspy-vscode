using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AssemblyNodeProvider(
    DecompilerBackend decompilerBackend,
    ReferencesRootNodeProvider referencesRootNodeProvider,
    NamespaceNodeProvider namespaceNodeProvider)
    : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return decompilerBackend.GetCode(
            nodeMetadata.AssemblyPath, EntityHandle.AssemblyDefinition, outputLanguage);
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.Assembly)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        return Task.FromResult(
            new[] { referencesRootNodeProvider.CreateNode(nodeMetadata.AssemblyPath) }
                .Concat(namespaceNodeProvider.CreateNodes(nodeMetadata.AssemblyPath)));
    }

    public static Node CreateNode(AssemblyData assemblyData)
    {
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyData.FilePath,
                Type = NodeType.Assembly,
                Name = Path.GetFileName(assemblyData.FilePath),
                IsDecompilable = true
            },
            DisplayName = GetAssemblyDisplayText(assemblyData),
            Description = assemblyData.FilePath,
            MayHaveChildren = true,
            SymbolModifiers = SymbolModifiers.None,
            Flags = NodeFlagsHelper.GetNodeFlags(assemblyData)
        };
    }

    private static string GetAssemblyDisplayText(AssemblyData assemblyData)
    {
        return string.Join(", ",
            new[] { assemblyData.Name, assemblyData.Version, assemblyData.TargetFramework }
                .Where(d => d is not null));
    }
}