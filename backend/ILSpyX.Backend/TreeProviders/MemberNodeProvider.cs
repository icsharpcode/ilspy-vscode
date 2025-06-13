using ICSharpCode.Decompiler.TypeSystem;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class MemberNodeProvider(DecompilerBackend decompilerBackend) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return decompilerBackend.GetCode(
            nodeMetadata.AssemblyPath,
            MetadataTokens.EntityHandle(nodeMetadata.SymbolToken),
            outputLanguage);
    }

    public Task<IEnumerable<Node>> CreateNodesAsync(string assemblyPath, int parentTypeSymbolToken)
    {
        var decompiler = decompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        var typeSystem = decompiler.TypeSystem;
        var typeDefinition = typeSystem.MainModule.GetDefinition(
            MetadataTokens.TypeDefinitionHandle(parentTypeSymbolToken));

        return Task.FromResult(
            typeDefinition == null
                ? Enumerable.Empty<Node>()
                : typeDefinition.NestedTypes
                    .Select(typeDefinition => new Node(
                        new NodeMetadata(
                            assemblyPath,
                            NodeTypeHelper.GetNodeTypeFromTypeKind(typeDefinition.Kind),
                            typeDefinition.TypeToString(false),
                            MetadataTokens.GetToken(typeDefinition.MetadataToken),
                            parentTypeSymbolToken),
                        typeDefinition.TypeToString(false),
                        "",
                        true,
                        NodeTypeHelper.GetSymbolModifiers(typeDefinition),
                        NodeFlagsHelper.GetNodeFlags(typeDefinition)
                    ))
                    .Union(typeDefinition.Fields.Select(field =>
                            CreateMemberNode(parentTypeSymbolToken, field, assemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                    .Union(typeDefinition.Properties.Select(property =>
                            CreateMemberNode(parentTypeSymbolToken, property, assemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                    .Union(typeDefinition.Events.Select(@event =>
                            CreateMemberNode(parentTypeSymbolToken, @event, assemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                    .Union(typeDefinition.Methods.Select(method =>
                            CreateMemberNode(parentTypeSymbolToken, method, assemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
        );
    }

    private static Node CreateMemberNode(int parentTypeSymbolToken, IMember member, string assemblyPath,
        ITypeDefinition typeDefinition)
    {
        string memberName = member is IMethod method
            ? method.MethodToString(false, false, false)
            : member.Name;
        return new Node(
            new NodeMetadata(
                assemblyPath,
                NodeTypeHelper.GetNodeTypeFromEntity(member),
                memberName,
                MetadataTokens.GetToken(member.MetadataToken),
                parentTypeSymbolToken),
            memberName,
            "",
            false,
            NodeTypeHelper.GetSymbolModifiers(member),
            NodeFlagsHelper.GetNodeFlags(member)
        );
    }
}