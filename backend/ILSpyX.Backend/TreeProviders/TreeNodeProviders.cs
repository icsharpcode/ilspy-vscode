using ICSharpCode.Decompiler.TypeSystem;
using ILSpyX.Backend.Application;
using ILSpyX.Backend.Model;

namespace ILSpyX.Backend.TreeProviders;

public class TreeNodeProviders(ILSpyXApplication application)
{
    private readonly ILSpyXApplication application = application;

    public ITreeNodeProvider ForNode(NodeMetadata? nodeMetadata) =>
        FromNodeType(nodeMetadata?.Type);

    public ITreeNodeProvider FromNodeType(NodeType? nodeType) => nodeType switch
    {
        null => AssemblyTreeRoot,
        NodeType.Assembly => Assembly,
        NodeType.ReferencesRoot => ReferencesRoot,
        NodeType.AssemblyReference => AssemblyReference,
        NodeType.Namespace => Namespace,
        var type when NodeTypeHelper.IsTypeNode(type.Value) => Type,
        var type when NodeTypeHelper.IsMemberNode(type.Value) => Member,
        NodeType.Analyzer => Analyzer,
        _ => Dummy
    };

    public ITreeNodeProvider FromSymbolKind(SymbolKind symbolKind) => symbolKind switch
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
        _ => Dummy,
    };

    public DummyTreeNodeProvider Dummy { get; } = new();
    public AssemblyTreeRootNodesProvider AssemblyTreeRoot { get; } = new(application);
    public AssemblyNodeProvider Assembly { get; } = new(application);
    public ReferencesRootNodeProvider ReferencesRoot { get; } = new(application);
    public AssemblyReferenceNodeProvider AssemblyReference { get; } = new(application);
    public NamespaceNodeProvider Namespace { get; } = new(application);
    public TypeNodeProvider Type { get; } = new(application);
    public MemberNodeProvider Member { get; } = new(application);
    public AnalyzersRootNodesProvider AnalyzersRoot { get; } = new(application);
    public AnalyzerNodeProvider Analyzer { get; } = new(application);
}

