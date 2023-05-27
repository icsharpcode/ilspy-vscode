using ICSharpCode.Decompiler.TypeSystem;
using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace ILSpy.Backend.TreeProviders;

public class MemberNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public MemberNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata)
    {
        return application.DecompilerBackend.GetCode(
            nodeMetadata.AssemblyPath,
            MetadataTokens.EntityHandle(nodeMetadata.SymbolToken));
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath, int parentTypeSymbolToken)
    {
        var decompiler = application.DecompilerBackend.GetDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return Enumerable.Empty<Node>();
        }

        var typeSystem = decompiler.TypeSystem;
        var typeDefinition = typeSystem.MainModule.GetDefinition(
            MetadataTokens.TypeDefinitionHandle(parentTypeSymbolToken));

        return typeDefinition == null
            ? Enumerable.Empty<Node>()
            : typeDefinition.NestedTypes
                .Select(typeDefinition => new Node(
                    new NodeMetadata(
                        AssemblyPath: assemblyPath,
                        Type: NodeTypeHelper.GetNodeTypeFromTypeKind(typeDefinition.Kind),
                        Name: typeDefinition.TypeToString(includeNamespace: false),
                        SymbolToken: MetadataTokens.GetToken(typeDefinition.MetadataToken),
                        ParentSymbolToken: parentTypeSymbolToken),
                    DisplayName: typeDefinition.TypeToString(includeNamespace: false),
                    Description: "",
                    MayHaveChildren: true,
                    SymbolModifiers: NodeTypeHelper.GetSymbolModifiers(typeDefinition)
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
                        .OrderBy(m => m.Metadata?.Name));
    }

    private static Node CreateMemberNode(int parentTypeSymbolToken, IMember member, string assemblyPath, ITypeDefinition typeDefinition)
    {
        string memberName = member is IMethod method
            ? method.MethodToString(false, false, false)
            : member.Name;
        return new Node(
            new NodeMetadata(
                AssemblyPath: assemblyPath,
                Type: NodeTypeHelper.GetNodeTypeFromEntity(member),
                Name: memberName,
                SymbolToken: MetadataTokens.GetToken(member.MetadataToken),
                ParentSymbolToken: parentTypeSymbolToken),
            DisplayName: memberName,
            Description: "",
            MayHaveChildren: false,
            SymbolModifiers: NodeTypeHelper.GetSymbolModifiers(member)
        );
    }
}

