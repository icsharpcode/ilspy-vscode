using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AssemblyTreeRootNodesProvider(ILSpyXApplication application) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return await application.TreeNodeProviders.Assembly.CreateNodesAsync();
    }
}

