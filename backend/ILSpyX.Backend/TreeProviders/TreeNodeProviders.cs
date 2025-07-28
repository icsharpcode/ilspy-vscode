using ICSharpCode.Decompiler.TypeSystem;
using ILSpyX.Backend.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;

// ReSharper disable ArrangeAccessorOwnerBody

namespace ILSpyX.Backend.TreeProviders;

public class TreeNodeProviders(IServiceProvider serviceProvider)
{
    public ITreeNodeProvider ForNode(NodeMetadata? nodeMetadata)
    {
        return FromNodeType(nodeMetadata?.Type);
    }

    private ITreeNodeProvider FromNodeType(NodeType? nodeType)
    {
        var providerType = nodeType switch
        {
            null => typeof(AssemblyTreeRootNodesProvider),
            NodeType.Assembly => typeof(AssemblyNodeProvider),
            NodeType.ReferencesRoot => typeof(ReferencesRootNodeProvider),
            NodeType.AssemblyReference => typeof(AssemblyReferenceNodeProvider),
            NodeType.Namespace => typeof(NamespaceNodeProvider),
            _ when NodeTypeHelper.IsTypeNode(nodeType.Value) => typeof(TypeNodeProvider),
            _ when NodeTypeHelper.IsMemberNode(nodeType.Value) => typeof(MemberNodeProvider),
            NodeType.Analyzer => typeof(AnalyzerCollector),
            NodeType.BaseTypes => typeof(BaseTypesNodeProvider),
            NodeType.DerivedTypes => typeof(DerivedTypesNodeProvider),
            _ => typeof(DummyTreeNodeProvider)
        };

        return GetProvider(providerType);
    }

    public ITreeNodeProvider FromSymbolKind(SymbolKind symbolKind)
    {
        var providerType = symbolKind switch
        {
            SymbolKind.TypeDefinition => typeof(TypeNodeProvider),
            SymbolKind.Field => typeof(MemberNodeProvider),
            SymbolKind.Property => typeof(MemberNodeProvider),
            SymbolKind.Indexer => typeof(MemberNodeProvider),
            SymbolKind.Event => typeof(MemberNodeProvider),
            SymbolKind.Method => typeof(MemberNodeProvider),
            SymbolKind.Operator => typeof(MemberNodeProvider),
            SymbolKind.Constructor => typeof(MemberNodeProvider),
            SymbolKind.Destructor => typeof(MemberNodeProvider),
            SymbolKind.Namespace => typeof(NamespaceNodeProvider),
            _ => typeof(DummyTreeNodeProvider)
        };

        return GetProvider(providerType);
    }

    private ITreeNodeProvider GetProvider(Type providerType)
    {
        return (serviceProvider.GetRequiredService(providerType) as ITreeNodeProvider) ??
               serviceProvider.GetRequiredService<DummyTreeNodeProvider>();
    }
}