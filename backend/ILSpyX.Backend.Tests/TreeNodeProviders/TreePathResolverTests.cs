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
}