using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class NuGetPackageNodeProvider(
    SingleThreadAssemblyList assemblyList,
    PackageFolderNodeProvider packageFolderNodeProvider) : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return Task.FromResult(DecompileResult.Empty());
    }

    public static Node CreateNode(AssemblyData assemblyData)
    {
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyData.FilePath,
                Type = NodeType.NuGetPackage,
                Name = Path.GetFileName(assemblyData.FilePath),
                IsDecompilable = true
            },
            DisplayName = assemblyData.Name,
            Description = assemblyData.FilePath,
            MayHaveChildren = true,
            SymbolModifiers = SymbolModifiers.None,
            Flags = NodeFlagsHelper.GetNodeFlags(assemblyData)
        };
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null)
        {
            return [];
        }

        var assembly = assemblyList.FindAssembly(nodeMetadata.AssemblyPath);
        if (assembly is null)
        {
            return [];
        }

        var package = (await assembly.GetLoadResultAsync()).Package;
        if (package is null)
        {
            return [];
        }

        return await packageFolderNodeProvider.GetPackageFolderChildrenAsync(nodeMetadata.AssemblyPath,
            package.RootFolder);
    }
}