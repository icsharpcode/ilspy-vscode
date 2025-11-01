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
                ? []
                : typeDefinition.NestedTypes
                    .Select(nestedTypeDefinition => new Node
                    {
                        Metadata = new NodeMetadata
                        {
                            AssemblyPath = assemblyPath,
                            Type = NodeTypeHelper.GetNodeTypeFromTypeKind(nestedTypeDefinition.Kind),
                            Name = nestedTypeDefinition.TypeToString(false),
                            SymbolToken = MetadataTokens.GetToken(nestedTypeDefinition.MetadataToken),
                            ParentSymbolToken = parentTypeSymbolToken,
                            IsDecompilable = true
                        },
                        DisplayName = nestedTypeDefinition.TypeToString(false),
                        Description = "",
                        MayHaveChildren = true,
                        SymbolModifiers = NodeTypeHelper.GetSymbolModifiers(nestedTypeDefinition),
                        Flags = NodeFlagsHelper.GetNodeFlags(nestedTypeDefinition)
                    })
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
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyPath,
                Type = NodeTypeHelper.GetNodeTypeFromEntity(member),
                Name = memberName,
                SymbolToken = MetadataTokens.GetToken(member.MetadataToken),
                ParentSymbolToken = parentTypeSymbolToken,
                IsDecompilable = true
            },
            DisplayName = memberName,
            Description = "",
            MayHaveChildren = false,
            SymbolModifiers = NodeTypeHelper.GetSymbolModifiers(member),
            Flags = NodeFlagsHelper.GetNodeFlags(member)
        };
    }
}