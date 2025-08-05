using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class DerivedTypesNodeProvider(SingleThreadAssemblyList assemblyList, DecompilerBackend decompilerBackend)
    : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public Node? CreateNode(NodeMetadata parentNodeMetadata)
    {
        var derivedTypes = FindDerivedTypes(parentNodeMetadata);
        if (!derivedTypes.ToBlockingEnumerable().Any())
        {
            return null;
        }
        
        return new Node(
            new NodeMetadata(
                parentNodeMetadata.AssemblyPath,
                NodeType.DerivedTypes,
                "Derived Types",
                parentNodeMetadata.SymbolToken,
                0),
            "Derived Types",
            string.Empty,
            true
        );
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return Task.FromResult(FindDerivedTypes(nodeMetadata)
            .ToBlockingEnumerable().Select(derivedType => {
                var typeNode =
                    TypeNodeProvider.CreateTypeNode(derivedType.ParentModule?.MetadataFile?.FileName ?? string.Empty,
                        derivedType);
                return typeNode with
                {
                    DisplayName = derivedType.FullName,
                    MayHaveChildren = false,
                    Metadata = typeNode.Metadata is not null
                        ? typeNode.Metadata with { IsDecompilable = false }
                        : null
                };
            }));
    }

    private async IAsyncEnumerable<ITypeDefinition> FindDerivedTypes(NodeMetadata? nodeMetadata)
    {
        if (assemblyList.AssemblyList is null || nodeMetadata is null)
        {
            yield break;
        }

        var typeDefinition = GetTypeDefinition(nodeMetadata);
        var definitionMetadata = typeDefinition?.ParentModule?.MetadataFile?.Metadata;
        if (typeDefinition is null || definitionMetadata is null)
        {
            yield break;
        }


        var metadataToken = (TypeDefinitionHandle) typeDefinition.MetadataToken;
        var assemblies = await assemblyList.AssemblyList.GetAllAssemblies();
        foreach (var loadedAssembly in assemblies)
        {
            var module = await loadedAssembly.GetMetadataFileOrNullAsync();
            var metadata = module?.Metadata;
            if (metadata is null || module?.GetTypeSystemOrNull()?.MainModule is not MetadataModule assembly)
            {
                continue;
            }

            foreach (var h in metadata.TypeDefinitions)
            {
                var td = metadata.GetTypeDefinition(h);
                foreach (var ifaceImpl in td.GetInterfaceImplementations()
                             .Select(iface => metadata.GetInterfaceImplementation(iface)).Where(ifaceImpl =>
                                 !ifaceImpl.Interface.IsNil &&
                                 IsSameType(metadata, ifaceImpl.Interface, definitionMetadata, metadataToken)))
                {
                    yield return assembly.GetDefinition(h);
                }

                var baseType = td.GetBaseTypeOrNil();
                if (!baseType.IsNil && IsSameType(metadata, baseType, definitionMetadata, metadataToken))
                {
                    yield return assembly.GetDefinition(h);
                }
            }
        }
    }

    private static bool IsSameType(MetadataReader referenceMetadata, EntityHandle typeRef,
        MetadataReader definitionMetadata, TypeDefinitionHandle typeDef)
    {
        return typeRef.GetFullTypeName(referenceMetadata) == typeDef.GetFullTypeName(definitionMetadata);
    }


    private ITypeDefinition? GetTypeDefinition(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null || assemblyList.AssemblyList is null)
        {
            return null;
        }

        var decompiler = decompilerBackend.CreateDecompiler(nodeMetadata.AssemblyPath);
        if (decompiler is null)
        {
            return null;
        }

        var typeSystem = decompiler.TypeSystem;
        return typeSystem.MainModule.GetDefinition(
            MetadataTokens.TypeDefinitionHandle(nodeMetadata.SymbolToken));
    }

}