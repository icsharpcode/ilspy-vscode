using ILSpyX.Backend.Model;
using ILSpyX.Backend.Search;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;

namespace ILSpyX.Backend.Tests;

public class SearchBackendTests
{
    private static async Task<SearchBackend> CreateSearchBackend()
    {
        var backendServices = await TestHelper.CreateTestServicesWithAssembly();
        return backendServices.GetRequiredService<SearchBackend>();
    }

    [Fact]
    public async Task ClassName()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("SomeClass", CancellationToken.None);
        Assert.Collection(nodeData,
            node => {
                Assert.Equal("SomeClass", node.DisplayName);
                Assert.Equal("TestAssembly", node.Description);
                Assert.Equal(NodeType.Class, node.Metadata?.Type);
                Assert.True(node.MayHaveChildren);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
            },
            node => {
                Assert.Equal("SomeClass.SomeClass()", node.DisplayName);
                Assert.Equal("TestAssembly.SomeClass", node.Description);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
                Assert.False(node.MayHaveChildren);
                Assert.Equal(SymbolModifiers.Static | SymbolModifiers.Private, node.SymbolModifiers);
            },
            node => {
                Assert.Equal("SomeClass.SomeClass()", node.DisplayName);
                Assert.Equal("TestAssembly.SomeClass", node.Description);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
                Assert.False(node.MayHaveChildren);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
            },
            node => {
                Assert.Equal("SomeClass.SomeClass(int)", node.DisplayName);
                Assert.Equal("TestAssembly.SomeClass", node.Description);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
                Assert.False(node.MayHaveChildren);
                Assert.Equal(SymbolModifiers.Internal, node.SymbolModifiers);
            }
        );
    }

    [Fact]
    public async Task InterfaceNameLowerCase()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("isomeinterface", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("ISomeInterface", node.DisplayName);
        Assert.Equal("TestAssembly", node.Description);
        Assert.Equal(NodeType.Interface, node.Metadata?.Type);
        Assert.True(node.MayHaveChildren);
        Assert.Equal(SymbolModifiers.Abstract | SymbolModifiers.Public, node.SymbolModifiers);
    }

    [Fact]
    public async Task EnumConstants()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("E1", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("SomeEnum.E1 : SomeEnum", node.DisplayName);
        Assert.Equal("TestAssembly.SomeEnum", node.Description);
        Assert.Equal(NodeType.Field, node.Metadata?.Type);
        Assert.False(node.MayHaveChildren);
        Assert.Equal(SymbolModifiers.Static | SymbolModifiers.Public, node.SymbolModifiers);
    }

    [Fact]
    public async Task VirtualClassMethod()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("VirtualMethod", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("SomeClass.VirtualMethod() : void", node.DisplayName);
        Assert.Equal("TestAssembly.SomeClass", node.Description);
        Assert.Equal(NodeType.Method, node.Metadata?.Type);
        Assert.False(node.MayHaveChildren);
        Assert.Equal(SymbolModifiers.Virtual | SymbolModifiers.Public, node.SymbolModifiers);
    }

    [Fact]
    public async Task OverrideClassMethod()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("ToString", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("SomeClass.ToString() : string", node.DisplayName);
        Assert.Equal("TestAssembly.SomeClass", node.Description);
        Assert.Equal(NodeType.Method, node.Metadata?.Type);
        Assert.False(node.MayHaveChildren);
        Assert.Equal(SymbolModifiers.Override | SymbolModifiers.Public, node.SymbolModifiers);
    }

    [Fact]
    public async Task Delegate()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("SomeDelegate", CancellationToken.None);
        Assert.Collection(nodeData,
            node => {
                Assert.Equal("SomeDelegate", node.DisplayName);
                Assert.Equal("TestAssembly", node.Description);
                Assert.Equal(NodeType.Delegate, node.Metadata?.Type);
                Assert.True(node.MayHaveChildren);
                Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Sealed, node.SymbolModifiers);
            },
            node => {
                Assert.Equal("SomeDelegate.SomeDelegate(object, nint)", node.DisplayName);
                Assert.Equal("TestAssembly.SomeDelegate", node.Description);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
                Assert.False(node.MayHaveChildren);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
            }
        );
    }

    [Fact]
    public async Task SearchOnlyInAddedAssemblies()
    {
        var application = await TestHelper.CreateTestServicesWithAssembly();
        var namespaceNodeProvider = application.GetRequiredService<NamespaceNodeProvider>();
        var typeNodeProvider = application.GetRequiredService<TypeNodeProvider>();
        var types = await namespaceNodeProvider.GetChildrenAsync(
            new NodeMetadata
            {
                AssemblyPath = TestHelper.AssemblyPath, Type = NodeType.Namespace, Name = "TestAssembly"
            });
        var typeNode = types.First(node => node.Metadata?.Name == "SomeClass");
        await typeNodeProvider.GetChildrenAsync(typeNode.Metadata);

        var searchBackend = application.GetRequiredService<SearchBackend>();
        var nodeData = await searchBackend.Search("String", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("SomeClass.ToString() : string", node.DisplayName);
        Assert.Equal("TestAssembly.SomeClass", node.Description);
        Assert.Equal(NodeType.Method, node.Metadata?.Type);
    }
}