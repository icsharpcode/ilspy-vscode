// Copyright (c) 2023 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Search;
using ILSpy.Backend.Model;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace ILSpy.Backend.Decompiler;

public class NodeProvider
{
    private readonly IDecompilerBackend decompilerBackend;

    public NodeProvider(IDecompilerBackend decompilerBackend)
    {
        this.decompilerBackend = decompilerBackend;
    }

    public IEnumerable<Node> GetNodes(NodeMetadata? nodeMetadata)
    {
        return nodeMetadata?.Type switch
        {
            null => GetAssemblyNodes(),
            NodeType.Assembly => GetAssemblyChildren(nodeMetadata),
            NodeType.ReferencesRoot => GetReferencesChildren(nodeMetadata),
            NodeType.Namespace => GetNamespaceChildren(nodeMetadata),
            NodeType.Class or NodeType.Enum or NodeType.Delegate or NodeType.Interface or NodeType.Struct =>
                GetTypeChildren(nodeMetadata),
            _ => Enumerable.Empty<Node>()
        };
    }

    private IEnumerable<Node> GetAssemblyNodes()
    {
        return decompilerBackend.GetLoadedAssemblies()
            .Select(assemblyData =>
                new Node(
                    new NodeMetadata(
                        AssemblyPath: assemblyData.FilePath,
                        Type: NodeType.Assembly,
                        Name: Path.GetFileName(assemblyData.FilePath),
                        SymbolToken: 0,
                        ParentSymbolToken: 0),
                    DisplayName: GetAssemblyDisplayText(assemblyData),
                    Description: assemblyData.FilePath,
                    MayHaveChildren: true,
                    SymbolModifiers: SymbolModifiers.None
                ));
    }

    private static string GetAssemblyDisplayText(AssemblyData assemblyData)
    {
        return string.Join(", ",
            new[] { assemblyData.Name, assemblyData.Version, assemblyData.TargetFramework }
            .Where(d => d is not null));
    }

    private IEnumerable<Node> GetAssemblyChildren(NodeMetadata nodeMetadata)
    {
        var decompiler = decompilerBackend.GetDecompiler(nodeMetadata.AssemblyPath);
        if ((nodeMetadata.Type != NodeType.Assembly) || decompiler is null)
        {
            return Enumerable.Empty<Node>();
        }

        var types = decompiler.TypeSystem.MainModule.TopLevelTypeDefinitions;
        HashSet<string> namespaces = new(decompiler.TypeSystem.NameComparer);
        foreach (var type in types)
        {
            namespaces.Add(type.Namespace);
        }
        return CreateReferencesRootNode(nodeMetadata).Concat(
            namespaces
                .OrderBy(n => n)
                .Select(ns => new Node(
                        new NodeMetadata(
                            AssemblyPath: nodeMetadata.AssemblyPath,
                            Type: NodeType.Namespace,
                            Name: ns,
                            SymbolToken: 0,
                            ParentSymbolToken: 0),
                        DisplayName: ns,
                        Description: string.Empty,
                        MayHaveChildren: true,
                        SymbolModifiers: SymbolModifiers.None
                    )));
    }

    private IEnumerable<Node> CreateReferencesRootNode(NodeMetadata nodeMetadata)
    {
        yield return new Node(
                new NodeMetadata(
                    AssemblyPath: nodeMetadata.AssemblyPath,
                    Type: NodeType.ReferencesRoot,
                    Name: "References",
                    SymbolToken: 0,
                    ParentSymbolToken: 0),
                DisplayName: "References",
                Description: string.Empty,
                MayHaveChildren: true,
                SymbolModifiers: SymbolModifiers.None
            );
    }

    private IEnumerable<Node> GetReferencesChildren(NodeMetadata nodeMetadata)
    {
        var decompiler = decompilerBackend.GetDecompiler(nodeMetadata.AssemblyPath);
        if ((nodeMetadata.Type != NodeType.ReferencesRoot) || decompiler is null)
        {
            return Enumerable.Empty<Node>();
        }

        HashSet<string> references = new(decompiler.TypeSystem.NameComparer);
        foreach (var ar in decompiler.TypeSystem.MainModule.PEFile.AssemblyReferences)
        {
            references.Add(ar.FullName);
        }
        return references
            .OrderBy(n => n)
            .Select(reference => new Node(
                    new NodeMetadata(
                        AssemblyPath: nodeMetadata.AssemblyPath,
                        Type: NodeType.AssemblyReference,
                        Name: reference,
                        SymbolToken: 0,
                        ParentSymbolToken: 0),
                    DisplayName: reference,
                    Description: string.Empty,
                    MayHaveChildren: false,
                    SymbolModifiers: SymbolModifiers.None
                )); ;
    }

    private IEnumerable<Node> GetNamespaceChildren(NodeMetadata nodeMetadata)
    {
        var decompiler = decompilerBackend.GetDecompiler(nodeMetadata.AssemblyPath);
        if ((nodeMetadata.Type != NodeType.Namespace) || decompiler is null)
        {
            yield break;
        }

        var currentNamespace = decompiler.TypeSystem.MainModule.RootNamespace;
        string[] parts = nodeMetadata.Name.Split('.');

        if (!(parts.Length == 1 && string.IsNullOrEmpty(parts[0])))
        {
            foreach (var part in parts)
            {
                var nested = currentNamespace.GetChildNamespace(part);
                if (nested == null)
                    yield break;
                currentNamespace = nested;
            }
        }

        foreach (var t in currentNamespace.Types.OrderBy(t => t.FullName))
        {
            var name = t.TypeToString(includeNamespace: false);
            yield return new Node(
                    new NodeMetadata(
                        AssemblyPath: nodeMetadata.AssemblyPath,
                        Type: GetNodeTypeFromTypeKind(t.Kind),
                        Name: name,
                        SymbolToken: MetadataTokens.GetToken(t.MetadataToken),
                        ParentSymbolToken: 0),
                    DisplayName: name,
                    Description: "",
                    MayHaveChildren: true,
                    SymbolModifiers: GetSymbolModifiers(t)
                );
        }
    }

    private IEnumerable<Node> GetTypeChildren(NodeMetadata nodeMetadata)
    {
        var decompiler = decompilerBackend.GetDecompiler(nodeMetadata.AssemblyPath);
        if (decompiler is null)
        {
            return Enumerable.Empty<Node>();
        }

        var typeSystem = decompiler.TypeSystem;
        var typeDefinition = typeSystem.MainModule.GetDefinition(
            MetadataTokens.TypeDefinitionHandle(nodeMetadata.SymbolToken));

        return typeDefinition == null
            ? Enumerable.Empty<Node>()
            : typeDefinition.NestedTypes
                .Select(typeDefinition => new Node(
                    new NodeMetadata(
                        AssemblyPath: nodeMetadata.AssemblyPath,
                        Type: GetNodeTypeFromTypeKind(typeDefinition.Kind),
                        Name: typeDefinition.TypeToString(includeNamespace: false),
                        SymbolToken: MetadataTokens.GetToken(typeDefinition.MetadataToken),
                        ParentSymbolToken: nodeMetadata.SymbolToken),
                    DisplayName: typeDefinition.TypeToString(includeNamespace: false),
                    Description: "",
                    MayHaveChildren: true,
                    SymbolModifiers: GetSymbolModifiers(typeDefinition)
                ))
                .Union(typeDefinition.Fields.Select(field =>
                    CreateMemberNode(nodeMetadata, field, nodeMetadata.AssemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                .Union(typeDefinition.Properties.Select(property =>
                    CreateMemberNode(nodeMetadata, property, nodeMetadata.AssemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                .Union(typeDefinition.Events.Select(@event =>
                    CreateMemberNode(nodeMetadata, @event, nodeMetadata.AssemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name))
                .Union(typeDefinition.Methods.Select(method =>
                    CreateMemberNode(nodeMetadata, method, nodeMetadata.AssemblyPath, typeDefinition))
                        .OrderBy(m => m.Metadata?.Name));
    }

    private static Node CreateMemberNode(NodeMetadata typeNodeMetadata, IMember member, string assemblyPath, ITypeDefinition typeDefinition)
    {
        string memberName = member is IMethod method
            ? method.MethodToString(false, false, false)
            : member.Name;
        return new Node(
            new NodeMetadata(
                AssemblyPath: assemblyPath,
                Type: GetNodeTypeFromEntity(member),
                Name: memberName,
                SymbolToken: MetadataTokens.GetToken(member.MetadataToken),
                ParentSymbolToken: typeNodeMetadata.SymbolToken),
            DisplayName: memberName,
            Description: "",
            MayHaveChildren: false,
            SymbolModifiers: GetSymbolModifiers(member)
        );
    }

    public static NodeType GetNodeTypeFromTypeKind(TypeKind typeKind)
    {
        return typeKind switch
        {
            TypeKind.Class => NodeType.Class,
            TypeKind.Delegate => NodeType.Delegate,
            TypeKind.Enum => NodeType.Enum,
            TypeKind.Interface => NodeType.Interface,
            TypeKind.Struct => NodeType.Struct,
            _ => NodeType.Unknown
        };
    }

    public static NodeType GetNodeTypeFromEntity(IEntity entity) => entity switch
    {
        ITypeDefinition typeDefinition => GetNodeTypeFromTypeKind(typeDefinition.Kind),
        IMethod => NodeType.Method,
        IField => NodeType.Field,
        IEvent => NodeType.Event,
        IProperty => NodeType.Property,
        _ => NodeType.Unknown
    };

    public static SymbolModifiers GetSymbolModifiers(IEntity entity)
    {
        SymbolModifiers modifiers = SymbolModifiers.None;

        switch (entity)
        {
            case ITypeDefinition typeDefinition:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, typeDefinition.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, typeDefinition.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, typeDefinition.IsReadOnly);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, typeDefinition.IsSealed);
                break;

            case IField field:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, field.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, field.IsVirtual);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Override, field.IsOverride);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, field.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, field.IsSealed);
                MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, field.IsReadOnly);
                break;

            case IMember member:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, member.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, member.IsVirtual);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Override, member.IsOverride);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, member.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, member.IsSealed);
                break;
        }

        MapSymbolModifierFromAccessibility(ref modifiers, entity.Accessibility);

        return modifiers;
    }

    private static void MapSymbolModifier(ref SymbolModifiers modifiers, SymbolModifiers modifier, bool condition)
    {
        if (condition)
        {
            modifiers |= modifier;
        }
    }

    private static void MapSymbolModifierFromAccessibility(ref SymbolModifiers modifiers, Accessibility accessibility)
    {
        switch (accessibility)
        {
            case Accessibility.Private:
                modifiers |= SymbolModifiers.Private;
                break;
            case Accessibility.ProtectedAndInternal:
                modifiers |= SymbolModifiers.Protected | SymbolModifiers.Private;
                break;
            case Accessibility.Protected:
                modifiers |= SymbolModifiers.Protected;
                break;
            case Accessibility.Internal:
                modifiers |= SymbolModifiers.Internal;
                break;
            case Accessibility.ProtectedOrInternal:
                modifiers |= SymbolModifiers.Protected | SymbolModifiers.Internal;
                break;
            case Accessibility.Public:
                modifiers |= SymbolModifiers.Public;
                break;
            default:
                break;
        }
    }
}

