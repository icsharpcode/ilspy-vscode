using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AssemblyNodeProvider(DecompilerBackend decompilerBackend, TreeNodeProviders treeNodeProviders)
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
            new[] { treeNodeProviders.ReferencesRoot.CreateNode(nodeMetadata.AssemblyPath) }
                .Concat(treeNodeProviders.Namespace.CreateNodes(nodeMetadata.AssemblyPath)));
    }

    public async Task<IEnumerable<Node>> CreateNodesAsync()
    {
        return (await decompilerBackend.GetLoadedAssembliesAsync())
            .Select(assemblyData =>
                new Node(
                    new NodeMetadata(
                        assemblyData.FilePath,
                        NodeType.Assembly,
                        Path.GetFileName(assemblyData.FilePath),
                        0,
                        0),
                    GetAssemblyDisplayText(assemblyData),
                    assemblyData.FilePath,
                    true,
                    SymbolModifiers.None,
                    NodeFlagsHelper.GetNodeFlags(assemblyData)
                ));
    }

    private static string GetAssemblyDisplayText(AssemblyData assemblyData)
    {
        return string.Join(", ",
            new[] { assemblyData.Name, assemblyData.Version, assemblyData.TargetFramework }
                .Where(d => d is not null));
    }
}