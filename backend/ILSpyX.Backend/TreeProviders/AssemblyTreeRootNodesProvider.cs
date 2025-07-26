using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AssemblyTreeRootNodesProvider(AssemblyNodeProvider assemblyNodeProvider) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return await assemblyNodeProvider.CreateNodesAsync();
    }
}