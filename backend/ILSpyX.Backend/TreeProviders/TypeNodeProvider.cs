using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class TypeNodeProvider(ILSpyXApplication application) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return application.DecompilerBackend.GetCode(
            nodeMetadata.AssemblyPath,
            MetadataTokens.EntityHandle(nodeMetadata.SymbolToken),
            outputLanguage);
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath, string @namespace)
    {
        var decompiler = application.DecompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            yield break;
        }

        var currentNamespace = decompiler.TypeSystem.MainModule.RootNamespace;
        string[] parts = @namespace.Split('.');

        if (!(parts.Length == 1 && string.IsNullOrEmpty(parts[0])))
        {
            foreach (var part in parts)
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
            var name = t.TypeToString(includeNamespace: false);
            yield return new Node(
                    new NodeMetadata(
                        AssemblyPath: assemblyPath,
                        Type: NodeTypeHelper.GetNodeTypeFromTypeKind(t.Kind),
                        Name: name,
                        SymbolToken: MetadataTokens.GetToken(t.MetadataToken),
                        ParentSymbolToken: 0),
                    DisplayName: name,
                    Description: "",
                    MayHaveChildren: true,
                    SymbolModifiers: NodeTypeHelper.GetSymbolModifiers(t)
                );
        }
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null || !NodeTypeHelper.IsTypeNode(nodeMetadata.Type))
        {
            return Enumerable.Empty<Node>();
        }

        return await application.TreeNodeProviders.Member.CreateNodesAsync(
            nodeMetadata.AssemblyPath, nodeMetadata.SymbolToken);
    }
}
