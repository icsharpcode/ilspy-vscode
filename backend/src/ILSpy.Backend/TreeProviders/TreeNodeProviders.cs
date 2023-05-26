using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;

namespace ILSpy.Backend.TreeProviders;

public class TreeNodeProviders
{
    private readonly ILSpyXApplication application;

    public ITreeNodeProvider ForNode(NodeMetadata? nodeMetadata) =>
        FromNodeType(nodeMetadata?.Type);

    public ITreeNodeProvider FromNodeType(NodeType? nodeType) => nodeType switch
    {
        null => Root,
        NodeType.Assembly => Assembly,
        NodeType.ReferencesRoot => ReferencesRoot,
        NodeType.AssemblyReference => AssemblyReference,
        NodeType.Namespace => Namespace,
        var type when NodeTypeHelper.IsTypeNode(type.Value) => Type,
        var type when NodeTypeHelper.IsMemberNode(type.Value) => Member,
        _ => throw new NotImplementedException($"No support for node type '{nodeType}'")
    };

    public TreeNodeProviders(ILSpyXApplication application)
    {
        this.application = application;

        Root = new(application);
        Assembly = new(application);
        ReferencesRoot = new(application);
        AssemblyReference = new(application);
        Namespace = new(application);
        Type = new(application);
        Member = new(application);
    }

    public RootNodesProvider Root { get; }
    public AssemblyNodeProvider Assembly { get; }
    public ReferencesRootNodeProvider ReferencesRoot { get; }
    public AssemblyReferenceNodeProvider AssemblyReference { get; }
    public NamespaceNodeProvider Namespace { get; }
    public TypeNodeProvider Type { get; }
    public MemberNodeProvider Member { get; }
}

