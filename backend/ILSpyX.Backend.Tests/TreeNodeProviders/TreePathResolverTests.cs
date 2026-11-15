using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;

namespace ILSpyX.Backend.Tests;

public class TreePathResolverTests
{
    [Fact]
    public async Task PathForAssemblyRootNodes()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var list = await services.GetRequiredService<AssemblyTreeRootNodesProvider>().GetChildrenAsync(null);
        var node = Assert.Single(list);
        var path = await services.GetRequiredService<TreePathResolver>().ResolveNodePathAsync(node.Metadata);
        Assert.NotNull(path);
        Assert.Empty(path);
    }


    [Fact]
    public async Task PathForNamespaceNode()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath, Type = NodeType.Namespace, Name = "A.B.C.D"
        };
        var path = await services.GetRequiredService<TreePathResolver>().ResolveNodePathAsync(nodeMetadata);
        Assert.NotNull(path);
        var node = Assert.Single(path);
        Assert.Equal("TestAssembly, 1.0.0.0, .NETCoreApp, v10.0", node.DisplayName);
        Assert.Equal(TestHelper.AssemblyPath, node.Description);
        Assert.Equal(NodeType.Assembly, node.Metadata?.Type);
    }

    [Fact]
    public async Task PathForMemberNode()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        int typeToken = await TestHelper.GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly",
            "SomeClass");
        int memberToken = await
            TestHelper.GetMemberToken(services.GetRequiredService<DecompilerBackend>(), typeToken, "ProgId");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath,
            Type = NodeType.Property,
            Name = "",
            SymbolToken = memberToken,
            ParentSymbolToken = typeToken
        };


        var path = await services.GetRequiredService<TreePathResolver>().ResolveNodePathAsync(nodeMetadata);
        Assert.NotNull(path);
        Assert.Collection(path,
            node => {
                Assert.Equal("TestAssembly, 1.0.0.0, .NETCoreApp, v10.0", node.DisplayName);
                Assert.Equal(TestHelper.AssemblyPath, node.Description);
                Assert.Equal(NodeType.Assembly, node.Metadata?.Type);
            },
            node => {
                Assert.Equal("TestAssembly", node.Metadata?.Name);
                Assert.Equal("TestAssembly", node.DisplayName);
                Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                Assert.True(node.MayHaveChildren);
                Assert.True(node.HasCommand(AvailableNodeCommands.Decompile));
            },
            node => {
                Assert.Equal("SomeClass", node.Metadata?.Name);
                Assert.Equal("SomeClass", node.DisplayName);
                Assert.Equal(NodeType.Class, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
            });
    }

    [Fact]
    public async Task PathForNestedTypeNode()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        int typeToken = await TestHelper.GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        int nestedTypeToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "NestedC");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath,
            Type = NodeType.Class,
            Name = "NestedC",
            SymbolToken = nestedTypeToken,
            ParentSymbolToken = typeToken
        };

        var path = await services.GetRequiredService<TreePathResolver>().ResolveNodePathAsync(nodeMetadata);
        Assert.NotNull(path);
        Assert.Collection(path,
            node => Assert.Equal(NodeType.Assembly, node.Metadata?.Type),
            node => {
                Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                Assert.Equal("TestAssembly", node.Metadata?.Name);
            },
            node => {
                Assert.Equal("SomeClass", node.Metadata?.Name);
                Assert.Equal(typeToken, node.Metadata?.SymbolToken);
                Assert.Equal(0, node.Metadata?.ParentSymbolToken);
            });
    }

    [Fact]
    public async Task PathForMemberOfNestedTypeMatchesTreeNodes()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var treeNodeProviders = services.GetRequiredService<TreeNodeProviders>();
        int typeToken = await TestHelper.GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        int nestedTypeToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "NestedC");
        int memberToken = await TestHelper.GetMemberToken(decompilerBackend, nestedTypeToken, "M() : void");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath,
            Type = NodeType.Method,
            Name = "M",
            SymbolToken = memberToken,
            ParentSymbolToken = nestedTypeToken
        };

        var path = (await services.GetRequiredService<TreePathResolver>().ResolveNodePathAsync(nodeMetadata))?.ToList();
        Assert.NotNull(path);
        Assert.Equal(4, path.Count);
        var nestedTypeNode = path[3];
        Assert.Equal(nestedTypeToken, nestedTypeNode.Metadata?.SymbolToken);
        Assert.Equal(typeToken, nestedTypeNode.Metadata?.ParentSymbolToken);

        // Every node in the path must be one of the children of its predecessor, so the tree can reveal it.
        NodeMetadata? parentMetadata = null;
        foreach (var pathNode in path.Append(new Node
                 {
                     Metadata = nodeMetadata, DisplayName = "", Description = "", MayHaveChildren = false
                 }))
        {
            var children = await treeNodeProviders.ForNode(parentMetadata).GetChildrenAsync(parentMetadata);
            Assert.Contains(children, child => IsSameTreeNode(child.Metadata, pathNode.Metadata));
            parentMetadata = pathNode.Metadata;
        }
    }

    // Same identity the extension uses for tree item IDs (see createNodeId), plus the name for token-less nodes.
    private static bool IsSameTreeNode(NodeMetadata? a, NodeMetadata? b) =>
        a is not null && b is not null &&
        a.AssemblyPath == b.AssemblyPath &&
        a.BundledAssemblyName == b.BundledAssemblyName &&
        a.Type == b.Type &&
        a.SymbolToken == b.SymbolToken &&
        a.ParentSymbolToken == b.ParentSymbolToken &&
        (a.SymbolToken != 0 || a.Name == b.Name);
}
