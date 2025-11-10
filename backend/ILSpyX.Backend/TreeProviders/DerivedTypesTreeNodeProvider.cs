using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System;
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
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return Task.FromResult(DecompileResult.Empty());
    }

    public Node? CreateNode(NodeMetadata parentNodeMetadata)
    {
        var derivedTypes = FindDerivedTypes(parentNodeMetadata);
        if (!derivedTypes.ToBlockingEnumerable().Any())
        {
            return null;
        }

        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = parentNodeMetadata.AssemblyPath,
                BundleSubPath = parentNodeMetadata.BundleSubPath,
                Type = NodeType.DerivedTypes,
                Name = "Derived Types",
                SymbolToken = parentNodeMetadata.SymbolToken,
                ParentSymbolToken = 0
            },
            DisplayName = "Derived Types",
            Description = string.Empty,
            MayHaveChildren = true
        };
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return Task.FromResult(FindDerivedTypes(nodeMetadata)
            .ToBlockingEnumerable().Select(derivedType => {
                var metadataFile = derivedType.ParentModule?.MetadataFile;
                var assemblyFileIdentifier = derivedType.ParentModule?.MetadataFile?.GetAssemblyFileIdentifier();
                if (assemblyFileIdentifier is null)
                {
                    return null;
                }
                var typeNode =
                    TypeNodeProvider.CreateTypeNode(assemblyFileIdentifier, derivedType);
                return typeNode with
                {
                    DisplayName = derivedType.FullName, MayHaveChildren = false
                };
            }).OfType<Node>());
    }

    private async IAsyncEnumerable<ITypeDefinition> FindDerivedTypes(NodeMetadata? nodeMetadata)
    {
        if (assemblyList.AssemblyList is null || nodeMetadata is null)
        {
            yield break;
        }

        var typeDefinition = await GetTypeDefinition(nodeMetadata);
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
                foreach (var _ in td.GetInterfaceImplementations()
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


    private async Task<ITypeDefinition?> GetTypeDefinition(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null || assemblyList.AssemblyList is null)
        {
            return null;
        }

        var decompiler = await decompilerBackend.CreateDecompiler(nodeMetadata.GetAssemblyFileIdentifier());
        var typeSystem = decompiler?.TypeSystem;
        return typeSystem?.MainModule.GetDefinition(
            MetadataTokens.TypeDefinitionHandle(nodeMetadata.SymbolToken));
    }

}