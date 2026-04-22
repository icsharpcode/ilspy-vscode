using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class TreePathResolver(TreeNodeProviders treeNodeProviders)
{
    public async Task<IEnumerable<Node>?> ResolveNodePathAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null)
        {
            return null;
        }

        List<Node> nodePath = [];
        var currentNodeMetadata = nodeMetadata;
        do
        {
            var parentNode = await treeNodeProviders.ForNode(currentNodeMetadata).FindParentAsync(currentNodeMetadata);
            currentNodeMetadata = parentNode?.Metadata;
            if (parentNode is not null)
            {
                nodePath.Insert(0, parentNode);
            }
        } while (currentNodeMetadata is not null);

        return nodePath;
    }
}