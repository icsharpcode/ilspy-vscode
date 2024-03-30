using ICSharpCode.ILSpyX;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using ILSpyX.Backend.Application;
using ILSpyX.Backend.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace ILSpy.Backend.Tests;

public class SearchBackendTests
{
    private static async Task<SearchBackend> CreateSearchBackend()
    {
        var assemblyListManager = new AssemblyListManager(new DummySettingsProvider());
        var assemblyList = new SingleThreadAssemblyList(assemblyListManager);
        var searchBackend = new SearchBackend(new NullLoggerFactory(), assemblyList, new ILSpyBackendSettings());
        await searchBackend.AddAssembly(
            Path.Combine(Path.GetDirectoryName(typeof(SearchBackendTests).Assembly.Location) ?? "", "TestAssembly.dll"));
        return searchBackend;
    }

    [Fact]
    public async Task ClassName()
    {
        var searchBackend = await CreateSearchBackend();
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
        var searchBackend = await CreateSearchBackend();
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
        var searchBackend = await CreateSearchBackend();
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
        var searchBackend = await CreateSearchBackend();
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
        var searchBackend = await CreateSearchBackend();
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
        var searchBackend = await CreateSearchBackend();
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
}
