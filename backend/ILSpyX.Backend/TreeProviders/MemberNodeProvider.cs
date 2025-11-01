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
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return decompilerBackend.GetCode(
            nodeMetadata.GetAssemblyFileIdentifier(),
            MetadataTokens.EntityHandle(nodeMetadata.SymbolToken),
            outputLanguage);
    }

    public async Task<IEnumerable<Node>> CreateNodesAsync(AssemblyFileIdentifier assemblyFile,
        int parentTypeSymbolToken)
    {
        var decompiler = await decompilerBackend.CreateDecompiler(assemblyFile);
        if (decompiler is null)
        {
            return [];
        }

        var typeSystem = decompiler.TypeSystem;
        var typeDefinition = typeSystem.MainModule.GetDefinition(
            MetadataTokens.TypeDefinitionHandle(parentTypeSymbolToken));

        return
            typeDefinition == null
                ? []
                : typeDefinition.NestedTypes
                    .Select(nestedTypeDefinition => new Node
                    {
                        Metadata = new NodeMetadata
                        {
                            AssemblyPath = assemblyFile.File,
                            BundleSubPath = assemblyFile.BundleSubPath,
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
                            CreateMemberNode(parentTypeSymbolToken, field, assemblyFile, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                    .Union(typeDefinition.Properties.Select(property =>
                            CreateMemberNode(parentTypeSymbolToken, property, assemblyFile, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                    .Union(typeDefinition.Events.Select(@event =>
                            CreateMemberNode(parentTypeSymbolToken, @event, assemblyFile, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                    .Union(typeDefinition.Methods.Select(method =>
                            CreateMemberNode(parentTypeSymbolToken, method, assemblyFile, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name));
    }

    private static Node CreateMemberNode(int parentTypeSymbolToken, IMember member, AssemblyFileIdentifier assemblyFile,
        ITypeDefinition typeDefinition)
    {
        string memberName = member is IMethod method
            ? method.MethodToString(false, false, false)
            : member.Name;
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyFile.File,
                BundleSubPath = assemblyFile.BundleSubPath,
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