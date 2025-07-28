using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class BaseTypesNodeProvider(SingleThreadAssemblyList assemblyList)
    : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        return Task.FromResult(GetBaseTypes(nodeMetadata.AssemblyPath, nodeMetadata.ParentSymbolToken)
            .Select(baseType => {
                var typeNode =
                    TypeNodeProvider.CreateTypeNode(baseType.ParentModule?.MetadataFile?.FileName ?? string.Empty,
                        baseType);
                return typeNode with
                {
                    MayHaveChildren = false,
                    Metadata = typeNode.Metadata is not null
                        ? typeNode.Metadata with { IsDecompilable = false }
                        : null
                };
            }));
    }

    public Node? CreateNode(string assemblyPath, int typeSymbolToken)
    {
        if (!GetBaseTypes(assemblyPath, typeSymbolToken).Any())
        {
            return null;
        }

        return new Node(
            new NodeMetadata(
                assemblyPath,
                NodeType.BaseTypes,
                "Base Types",
                0,
                typeSymbolToken),
            "Base Types",
            string.Empty,
            true
        );
    }

    private IEnumerable<ITypeDefinition> GetBaseTypes(string assemblyFile, int typeSymbolToken)
    {
        var module = assemblyList.FindAssembly(assemblyFile)?.GetMetadataFileOrNull();
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