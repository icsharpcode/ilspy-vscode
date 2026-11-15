// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Analyzers;
using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Navigation;

/// <summary>
/// Navigation from symbol references in decompiled code to their definitions and implementations.
/// </summary>
public class SymbolNavigationBackend(
    DecompilerBackend decompilerBackend,
    SingleThreadAssemblyList assemblyList,
    AnalyzerBackend analyzerBackend,
    DerivedTypesNodeProvider derivedTypesNodeProvider)
{
    // Locating a symbol in another document requires decompiling it, so only the first targets are located.
    private const int MaxLocatedTargets = 20;

    private static readonly HashSet<string> ImplementationAnalyzers =
    [
        "MethodImplementedByAnalyzer", "MethodOverriddenByAnalyzer",
        "PropertyImplementedByAnalyzer", "PropertyOverriddenByAnalyzer",
        "EventImplementedByAnalyzer", "EventOverriddenByAnalyzer"
    ];

    public async Task<IEnumerable<CodeLocation>> FindDefinitions(NodeMetadata nodeMetadata, string outputLanguage,
        int line, int column)
    {
        var (document, reference) = await FindReferenceAt(nodeMetadata, outputLanguage, line, column);
        if (document is null || reference is null)
        {
            return [];
        }

        if (reference.IsDefinition)
        {
            return [ToLocation(null, reference)];
        }

        if (reference.Target is LocalReferenceTarget)
        {
            return document.FindDefinition(reference.Target) is { } localDefinition
                ? [ToLocation(null, localDefinition)]
                : [];
        }

        var entity = await ResolveEntity(reference.Target);
        if (entity is null)
        {
            return [];
        }

        var location = await LocateEntity(entity, document, outputLanguage, true);
        return location is null ? [] : [location];
    }

    public async Task<IEnumerable<CodeLocation>> FindImplementations(NodeMetadata nodeMetadata,
        string outputLanguage, int line, int column, CancellationToken cancellationToken = default)
    {
        var (document, reference) = await FindReferenceAt(nodeMetadata, outputLanguage, line, column);
        if (document is null || reference is null || reference.Target is LocalReferenceTarget)
        {
            return [];
        }

        var entity = await ResolveEntity(reference.Target);
        if (entity is null)
        {
            return [];
        }

        var locations = new List<CodeLocation>();
        if (entity is ITypeDefinition typeDefinition)
        {
            if (typeDefinition.Kind != TypeKind.Interface && !typeDefinition.IsAbstract)
            {
                AddLocation(await LocateEntity(typeDefinition, document, outputLanguage, true));
            }

            if (CreateNode(typeDefinition) is { Metadata: { } typeNodeMetadata })
            {
                foreach (var derivedTypeNode in await derivedTypesNodeProvider.GetChildrenAsync(typeNodeMetadata))
                {
                    AddLocation(await LocateNode(derivedTypeNode, document, outputLanguage,
                        locations.Count < MaxLocatedTargets));
                }
            }
        }
        else
        {
            foreach (var implementation in FindMemberImplementations((IMember) entity, cancellationToken))
            {
                AddLocation(await LocateEntity(implementation, document, outputLanguage,
                    locations.Count < MaxLocatedTargets));
            }
        }

        return locations.DistinctBy(location => (location.Node?.Metadata, location.Line, location.Column));

        void AddLocation(CodeLocation? location)
        {
            if (location is not null)
            {
                locations.Add(location);
            }
        }
    }

    private async Task<(DecompiledDocument? Document, CodeReference? Reference)> FindReferenceAt(
        NodeMetadata nodeMetadata, string outputLanguage, int line, int column)
    {
        if (!IsTypeOrMember(nodeMetadata.Type))
        {
            return (null, null);
        }

        var document = await decompilerBackend.GetDecompiledDocument(
            nodeMetadata.GetAssemblyFileIdentifier(), MetadataTokens.EntityHandle(nodeMetadata.SymbolToken),
            outputLanguage);
        return (document, document?.FindReferenceAt(line, column));
    }

    private IEnumerable<IEntity> FindMemberImplementations(IMember member, CancellationToken cancellationToken)
    {
        if (!member.IsAbstract)
        {
            yield return member;
        }

        if (assemblyList.AssemblyList is null)
        {
            yield break;
        }

        var context = new AnalyzerContext
        {
            CancellationToken = cancellationToken,
            Language = new CSharpLanguage(),
            AssemblyList = assemblyList.AssemblyList
        };
        foreach (var analyzer in analyzerBackend.Analyzers.Where(analyzer =>
                     ImplementationAnalyzers.Contains(analyzer.NodeSubType) && analyzer.Instance.Show(member)))
        {
            foreach (var implementation in analyzer.Instance.Analyze(member, context).OfType<IEntity>())
            {
                yield return implementation;
            }
        }
    }

    /// <summary>
    /// Finds the location of the entity's definition, preferring the current document.
    /// </summary>
    private async Task<CodeLocation?> LocateEntity(IEntity entity, DecompiledDocument currentDocument,
        string outputLanguage, bool locateInOtherDocument)
    {
        var node = CreateNode(entity);
        return node is null
            ? null
            : await LocateNode(node, currentDocument, outputLanguage, locateInOtherDocument);
    }

    private async Task<CodeLocation?> LocateNode(Node node, DecompiledDocument currentDocument,
        string outputLanguage, bool locateInOtherDocument)
    {
        if (node.Metadata is not { } metadata)
        {
            return null;
        }

        var assembly = metadata.GetAssemblyFileIdentifier();
        var target = new SymbolReferenceTarget(assembly, metadata.SymbolToken);
        if (currentDocument.FindDefinition(target) is { } definitionInCurrentDocument)
        {
            return ToLocation(null, definitionInCurrentDocument);
        }

        if (!locateInOtherDocument)
        {
            return new CodeLocation(node, 0, 0, 0);
        }

        var document = await decompilerBackend.GetDecompiledDocument(
            assembly, MetadataTokens.EntityHandle(metadata.SymbolToken), outputLanguage);
        return document?.FindDefinition(target) is { } definition
            ? ToLocation(node, definition)
            : new CodeLocation(node, 0, 0, 0);
    }

    private async Task<IEntity?> ResolveEntity(ReferenceTarget target)
    {
        switch (target)
        {
            case SymbolReferenceTarget symbol:
                return await decompilerBackend.GetEntityFromHandle(
                    symbol.Assembly, MetadataTokens.EntityHandle(symbol.Token));

            case HandleReferenceTarget reference:
                var decompiler = await decompilerBackend.CreateDecompiler(reference.Assembly);
                if (decompiler is null)
                {
                    return null;
                }

                var module = decompiler.TypeSystem.MainModule;
                var handle = MetadataTokens.EntityHandle(reference.Token);
                return handle.Kind is HandleKind.TypeReference or HandleKind.TypeSpecification
                    ? module.ResolveType(handle, default).GetDefinition()
                    : (module.ResolveEntity(handle, default) as IMember)?.MemberDefinition;

            default:
                return null;
        }
    }

    /// <summary>
    /// Creates the tree node of a type or member, equal to the node shown in the assembly tree.
    /// </summary>
    private static Node? CreateNode(IEntity entity)
    {
        // Accessors are not shown in the tree, but their property or event.
        if (entity is IMethod { AccessorOwner: { } accessorOwner })
        {
            entity = accessorOwner;
        }

        var assembly = entity.ParentModule?.MetadataFile?.GetAssemblyFileIdentifier();
        if (assembly is null || entity.MetadataToken.IsNil)
        {
            return null;
        }

        return entity switch
        {
            ITypeDefinition typeDefinition => TypeNodeProvider.CreateTypeNode(assembly, typeDefinition),
            IMember { DeclaringTypeDefinition: { } declaringType } member => MemberNodeProvider.CreateMemberNode(
                MetadataTokens.GetToken(declaringType.MetadataToken), member, assembly, declaringType),
            _ => null
        };
    }

    private static bool IsTypeOrMember(NodeType nodeType) =>
        nodeType is NodeType.Class or NodeType.Interface or NodeType.Struct or NodeType.Enum or NodeType.Delegate
            or NodeType.Event or NodeType.Field or NodeType.Method or NodeType.Const or NodeType.Property;

    private static CodeLocation ToLocation(Node? node, CodeReference reference) =>
        new(node, reference.Line, reference.Column, reference.Length);
}
