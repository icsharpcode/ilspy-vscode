using ILSpy.Backend.Model;

namespace ILSpyX.Backend.Tests;

public class SearchBackendTests
{
    [Fact]
    public async Task ClassName()
    {
        var searchBackend = (await TestHelper.CreateTestApplication()).SearchBackend;
        var nodeData = await searchBackend.Search("SomeClass", new CancellationToken());
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
        var searchBackend = (await TestHelper.CreateTestApplication()).SearchBackend;
        var nodeData = await searchBackend.Search("isomeinterface", new CancellationToken());
        Assert.Collection(nodeData,
                node => {
                    Assert.Equal("ISomeInterface", node.DisplayName);
                    Assert.Equal("TestAssembly", node.Description);
                    Assert.Equal(NodeType.Interface, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.Abstract | SymbolModifiers.Public, node.SymbolModifiers);
                }
            );
    }

    [Fact]
    public async Task EnumConstants()
    {
        var searchBackend = (await TestHelper.CreateTestApplication()).SearchBackend;
        var nodeData = await searchBackend.Search("E1", new CancellationToken());
        Assert.Collection(nodeData,
                node => {
                    Assert.Equal("SomeEnum.E1 : SomeEnum", node.DisplayName);
                    Assert.Equal("TestAssembly.SomeEnum", node.Description);
                    Assert.Equal(NodeType.Field, node.Metadata?.Type);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.Static | SymbolModifiers.Public, node.SymbolModifiers);
                }
            );
    }

    [Fact]
    public async Task VirtualClassMethod()
    {
        var searchBackend = (await TestHelper.CreateTestApplication()).SearchBackend;
        var nodeData = await searchBackend.Search("VirtualMethod", new CancellationToken());
        Assert.Collection(nodeData,
                node => {
                    Assert.Equal("SomeClass.VirtualMethod() : void", node.DisplayName);
                    Assert.Equal("TestAssembly.SomeClass", node.Description);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.Virtual | SymbolModifiers.Public, node.SymbolModifiers);
                }
            );
    }

    [Fact]
    public async Task OverrideClassMethod()
    {
        var searchBackend = (await TestHelper.CreateTestApplication()).SearchBackend;
        var nodeData = await searchBackend.Search("ToString", new CancellationToken());
        Assert.Collection(nodeData,
                node => {
                    Assert.Equal("SomeClass.ToString() : string", node.DisplayName);
                    Assert.Equal("TestAssembly.SomeClass", node.Description);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.Override | SymbolModifiers.Public, node.SymbolModifiers);
                }
            );
    }

    [Fact]
    public async Task Delegate()
    {
        var searchBackend = (await TestHelper.CreateTestApplication()).SearchBackend;
        var nodeData = await searchBackend.Search("SomeDelegate", new CancellationToken());
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
        var application = await TestHelper.CreateTestApplication();
        var types = await application.TreeNodeProviders.Namespace.GetChildrenAsync(
            new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "TestAssembly", 0, 0));
        var typeNode = types.First(node => node.Metadata?.Name == "SomeClass");
        await application.TreeNodeProviders.Type.GetChildrenAsync(typeNode.Metadata);

        var searchBackend = application.SearchBackend;
        var nodeData = await searchBackend.Search("String", new CancellationToken());
        Assert.Collection(nodeData,
            node => {
                Assert.Equal("SomeClass.ToString() : string", node.DisplayName);
                Assert.Equal("TestAssembly.SomeClass", node.Description);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
            }
        );
    }
}
