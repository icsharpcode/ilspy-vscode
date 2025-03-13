using ICSharpCode.Decompiler.TypeSystem;
using ILSpyX.Backend.Model;
using Microsoft.Extensions.DependencyInjection;
using System;

// ReSharper disable ArrangeAccessorOwnerBody

namespace ILSpyX.Backend.TreeProviders;

public class TreeNodeProviders(IServiceProvider serviceProvider)
{
    public DummyTreeNodeProvider Dummy => serviceProvider.GetRequiredService<DummyTreeNodeProvider>();

    public AssemblyTreeRootNodesProvider AssemblyTreeRoot =>
        serviceProvider.GetRequiredService<AssemblyTreeRootNodesProvider>();

    public AssemblyNodeProvider Assembly =>
        serviceProvider.GetRequiredService<AssemblyNodeProvider>();

    public ReferencesRootNodeProvider ReferencesRoot =>
        serviceProvider.GetRequiredService<ReferencesRootNodeProvider>();

    public AssemblyReferenceNodeProvider AssemblyReference =>
        serviceProvider.GetRequiredService<AssemblyReferenceNodeProvider>();

    public NamespaceNodeProvider Namespace => serviceProvider.GetRequiredService<NamespaceNodeProvider>();
    public TypeNodeProvider Type => serviceProvider.GetRequiredService<TypeNodeProvider>();
    public MemberNodeProvider Member => serviceProvider.GetRequiredService<MemberNodeProvider>();
    public AnalyzersRootNodesProvider AnalyzersRoot => serviceProvider.GetRequiredService<AnalyzersRootNodesProvider>();
    public AnalyzerNodeProvider Analyzer => serviceProvider.GetRequiredService<AnalyzerNodeProvider>();

    public ITreeNodeProvider ForNode(NodeMetadata? nodeMetadata)
    {
        return FromNodeType(nodeMetadata?.Type);
    }

    public ITreeNodeProvider FromNodeType(NodeType? nodeType)
    {
        return nodeType switch
        {
            null => AssemblyTreeRoot,
            NodeType.Assembly => Assembly,
            NodeType.ReferencesRoot => ReferencesRoot,
            NodeType.AssemblyReference => AssemblyReference,
            NodeType.Namespace => Namespace,
            _ when NodeTypeHelper.IsTypeNode(nodeType.Value) => Type,
            _ when NodeTypeHelper.IsMemberNode(nodeType.Value) => Member,
            NodeType.Analyzer => Analyzer,
            _ => Dummy
        };
    }

    public ITreeNodeProvider FromSymbolKind(SymbolKind symbolKind)
    {
        return symbolKind switch
        {
            SymbolKind.TypeDefinition => Type,
            SymbolKind.Field => Member,
            SymbolKind.Property => Member,
            SymbolKind.Indexer => Member,
            SymbolKind.Event => Member,
            SymbolKind.Method => Member,
            SymbolKind.Operator => Member,
            SymbolKind.Constructor => Member,
            SymbolKind.Destructor => Member,
            SymbolKind.Namespace => Namespace,
            _ => Dummy
        };
    }
}