using ICSharpCode.ILSpyX;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace ILSpy.Backend.Tests;

public class SearchBackendTests
{
    private static SearchBackend CreateSearchBackend()
    {
        var assemblyListManager = new AssemblyListManager(new SettingsProvider());
        var searchBackend = new SearchBackend(new NullLoggerFactory(), assemblyListManager);
        searchBackend.AddAssembly(
            Path.Combine(Path.GetDirectoryName(typeof(SearchBackendTests).Assembly.Location) ?? "", "TestAssembly.dll"));
        return searchBackend;
    }

    [Fact]
    public async Task SearchForClassName()
    {
        var searchBackend = CreateSearchBackend();
        var nodeData = await searchBackend.Search("SomeClass", new CancellationToken());
        Assert.Collection(nodeData,
                node => {
                    Assert.Equal("SomeClass", node.Name);
                    Assert.Equal("TestAssembly", node.Description);
                    Assert.Equal(NodeType.Class, node.Node?.Type);
                    Assert.True(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.None, node.SymbolModifiers);
                },
                node => {
                    Assert.Equal("SomeClass.SomeClass()", node.Name);
                    Assert.Equal("TestAssembly.SomeClass", node.Description);
                    Assert.Equal(NodeType.Method, node.Node?.Type);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.None, node.SymbolModifiers);
                },
                node => {
                    Assert.Equal("SomeClass.SomeClass()", node.Name);
                    Assert.Equal("TestAssembly.SomeClass", node.Description);
                    Assert.Equal(NodeType.Method, node.Node?.Type);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.None, node.SymbolModifiers);
                },
                node => {
                    Assert.Equal("SomeClass.SomeClass(int)", node.Name);
                    Assert.Equal("TestAssembly.SomeClass", node.Description);
                    Assert.Equal(NodeType.Method, node.Node?.Type);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.None, node.SymbolModifiers);
                }
            );
    }

    [Fact]
    public async Task SearchForInterfaceNameLowerCase()
    {
        var searchBackend = CreateSearchBackend();
        var nodeData = await searchBackend.Search("isomeinterface", new CancellationToken());
        Assert.Collection(nodeData,
                node => {
                    Assert.Equal("ISomeInterface", node.Name);
                    Assert.Equal("TestAssembly", node.Description);
                    Assert.Equal(NodeType.Interface, node.Node?.Type);
                    Assert.True(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.None, node.SymbolModifiers);
                }
            );
    }

    [Fact]
    public async Task SearchForEnumConstants()
    {
        var searchBackend = CreateSearchBackend();
        var nodeData = await searchBackend.Search("E1", new CancellationToken());
        Assert.Collection(nodeData,
                node => {
                    Assert.Equal("SomeEnum.E1 : SomeEnum", node.Name);
                    Assert.Equal("TestAssembly.SomeEnum", node.Description);
                    Assert.Equal(NodeType.Field, node.Node?.Type);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(SymbolModifiers.None, node.SymbolModifiers);
                }
            );
    }
}
