using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AssemblyTreeRootNodesProvider(DecompilerBackend decompilerBackend) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        var test = await decompilerBackend.GetLoadedAssembliesAsync();
        return (await decompilerBackend.GetLoadedAssembliesAsync())
            .Select(data => data.PackageType switch
            {
                PackageType.NuGet => NuGetPackageNodeProvider.CreateNode(data),
                _ => AssemblyNodeProvider.CreateNode(data)
            });
    }
}