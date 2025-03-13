using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class TypeNodeProvider(TreeNodeProviders treeNodeProviders, DecompilerBackend decompilerBackend)
    : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return decompilerBackend.GetCode(
            nodeMetadata.AssemblyPath,
            MetadataTokens.EntityHandle(nodeMetadata.SymbolToken),
            outputLanguage);
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null || !NodeTypeHelper.IsTypeNode(nodeMetadata.Type))
        {
            return [];
        }

        return await treeNodeProviders.Member.CreateNodesAsync(
            nodeMetadata.AssemblyPath, nodeMetadata.SymbolToken);
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath, string @namespace)
    {
        var decompiler = decompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            yield break;
        }

        var currentNamespace = decompiler.TypeSystem.MainModule.RootNamespace;
        string[] parts = @namespace.Split('.');

        if (!(parts.Length == 1 && string.IsNullOrEmpty(parts[0])))
        {
            foreach (string part in parts)
            {
                var nested = currentNamespace.GetChildNamespace(part);
                if (nested == null)
                {
                    yield break;
                }

                currentNamespace = nested;
            }
        }

        foreach (var t in currentNamespace.Types.OrderBy(t => t.FullName))
        {
            string name = t.TypeToString(false);
            yield return new Node(
                new NodeMetadata(
                    assemblyPath,
                    NodeTypeHelper.GetNodeTypeFromTypeKind(t.Kind),
                    name,
                    MetadataTokens.GetToken(t.MetadataToken),
                    0),
                name,
                "",
                true,
                NodeTypeHelper.GetSymbolModifiers(t),
                NodeFlagsHelper.GetNodeFlags(t)
            );
        }
    }
}