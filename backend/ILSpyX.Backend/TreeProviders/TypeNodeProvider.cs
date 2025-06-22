using ICSharpCode.Decompiler.TypeSystem;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class TypeNodeProvider(
    MemberNodeProvider memberNodeProvider,
    BaseTypesNodeProvider baseTypesNodeProvider,
    DerivedTypesNodeProvider derivedTypesNodeProvider,
    DecompilerBackend decompilerBackend)
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

        IEnumerable<Node?> nodes =
        [
            baseTypesNodeProvider.CreateNode(nodeMetadata.AssemblyPath, nodeMetadata.SymbolToken),
            derivedTypesNodeProvider.CreateNode(nodeMetadata)
        ];

        return nodes.OfType<Node>().Concat(await memberNodeProvider.CreateNodesAsync(
            nodeMetadata.AssemblyPath, nodeMetadata.SymbolToken));
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath, string @namespace)
    {
        var decompiler = decompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return [];
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
                    return [];
                }

                currentNamespace = nested;
            }
        }

        return currentNamespace.Types.OrderBy(t => t.FullName).Select(t => CreateTypeNode(assemblyPath, t));
    }

    public static Node CreateTypeNode(string assemblyPath, ITypeDefinition typeDefinition)
    {
        string name = typeDefinition.TypeToString(false);
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyPath,
                Type = NodeTypeHelper.GetNodeTypeFromTypeKind(typeDefinition.Kind),
                Name = name,
                SymbolToken = MetadataTokens.GetToken(typeDefinition.MetadataToken),
                ParentSymbolToken = 0,
                IsDecompilable = true
            },
            DisplayName = name,
            Description = "",
            MayHaveChildren = true,
            SymbolModifiers = NodeTypeHelper.GetSymbolModifiers(typeDefinition),
            Flags = NodeFlagsHelper.GetNodeFlags(typeDefinition)
        };
    }
}