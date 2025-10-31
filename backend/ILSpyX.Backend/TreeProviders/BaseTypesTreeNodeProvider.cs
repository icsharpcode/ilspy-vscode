using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class BaseTypesNodeProvider(SingleThreadAssemblyList assemblyList)
    : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return Task.FromResult(DecompileResult.Empty());
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null)
        {
            return [];
        }

        var baseTypes = await GetBaseTypes(nodeMetadata.GetAssemblyFileIdentifier(), nodeMetadata.SymbolToken);
        return baseTypes
            .Select(baseType => {
                var metadataFile = baseType.ParentModule?.MetadataFile;
                LoadedAssembly? loadedAssembly;
                try
                {
                    loadedAssembly = metadataFile?.GetLoadedAssembly();
                }
                catch (Exception)
                {
                    loadedAssembly = null;
                }

                if (loadedAssembly is null)
                {
                    return null;
                }

                var typeNode = TypeNodeProvider.CreateTypeNode(loadedAssembly.GetAssembilyFileIdentifier(), baseType);
                return typeNode with { DisplayName = baseType.FullName, MayHaveChildren = false };
            }).OfType<Node>();
    }

    public async Task<Node?> CreateNode(AssemblyFileIdentifier assemblyFile, int typeSymbolToken)
    {
        var baseTypes = await GetBaseTypes(assemblyFile, typeSymbolToken);
        if (!baseTypes.Any())
        {
            return null;
        }

        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyFile.File,
                BundleSubPath = assemblyFile.BundleSubPath,
                Type = NodeType.BaseTypes,
                Name = "Base Types",
                SymbolToken = typeSymbolToken,
                ParentSymbolToken = 0
            },
            DisplayName = "Base Types",
            Description = string.Empty,
            MayHaveChildren = true
        };
    }

    private async Task<IEnumerable<ITypeDefinition>> GetBaseTypes(AssemblyFileIdentifier assemblyFile,
        int typeSymbolToken)
    {
        var assembly = await assemblyList.FindAssembly(assemblyFile);
        if (assembly is null)
        {
            return [];
        }

        var module = await assembly.GetMetadataFileOrNullAsync();
        if (module is null)
        {
            return [];
        }

        var typeDefinitionHandle = MetadataTokens.TypeDefinitionHandle(typeSymbolToken);
        var typeSystem = new DecompilerTypeSystem(module, module.GetAssemblyResolver(),
            TypeSystemOptions.Default | TypeSystemOptions.Uncached);
        if (typeSystem.MainModule.ResolveEntity(typeDefinitionHandle) is not ITypeDefinition typeDefinition)
        {
            return [];
        }

        return typeDefinition.GetAllBaseTypeDefinitions().Reverse().Skip(1).Where(baseType =>
            typeDefinition.Kind != TypeKind.Interface || typeDefinition.Kind == baseType.Kind);
    }
}