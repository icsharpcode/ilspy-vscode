using ILSpy.Backend.Model;
using ILSpyX.Backend.Tests;
using Mono.Cecil;

namespace ILSpy.Backend.Tests;

public class TreeNodeProviderTests
{
    [Fact]
    public async Task GetRootNodes()
    {
        var application = await TestHelper.CreateTestApplication();
        var list = await application.TreeNodeProviders.AssemblyTreeRoot.GetChildrenAsync(null);
        Assert.Collection(list,
                node => {
                    Assert.Equal("TestAssembly, 1.0.0.0, .NETCoreApp, v8.0", node.DisplayName);
                    Assert.Equal(TestHelper.AssemblyPath, node.Description);
                    Assert.True(node.MayHaveChildren);
                    Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
                    Assert.Equal(Path.GetFileName(TestHelper.AssemblyPath), node.Metadata?.Name);
                    Assert.Equal(NodeType.Assembly, node.Metadata?.Type);
                });
    }

    [Fact]
    public async Task GetAssemblyChildren()
    {
        var application = await TestHelper.CreateTestApplication();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Assembly, TestHelper.AssemblyPath, 0, 0);
        var list = await application.TreeNodeProviders.ForNode(nodeMetadata).GetChildrenAsync(nodeMetadata);
        Assert.Collection(list,
                node => {
                    Assert.Equal("References", node.Metadata?.Name);
                    Assert.Equal("References", node.DisplayName);
                    Assert.Equal(NodeType.ReferencesRoot, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("A", node.Metadata?.Name);
                    Assert.Equal("A", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("A.B", node.Metadata?.Name);
                    Assert.Equal("A.B", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("A.B.C", node.Metadata?.Name);
                    Assert.Equal("A.B.C", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("A.B.C.D", node.Metadata?.Name);
                    Assert.Equal("A.B.C.D", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("CSharpVariants", node.Metadata?.Name);
                    Assert.Equal("CSharpVariants", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("Generics", node.Metadata?.Name);
                    Assert.Equal("Generics", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("TestAssembly", node.Metadata?.Name);
                    Assert.Equal("TestAssembly", node.DisplayName);
                    Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
                    Assert.True(node.MayHaveChildren);
                });
    }

    [Fact]
    public async Task GetReferenceChildren()
    {
        var application = await TestHelper.CreateTestApplication();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.ReferencesRoot, "References", 0, 0);
        var list = await application.TreeNodeProviders.ForNode(nodeMetadata).GetChildrenAsync(nodeMetadata);
        Assert.Collection(list,
                node => {
                    Assert.StartsWith("System.Runtime", node.Metadata?.Name);
                    Assert.StartsWith("System.Runtime, Version=", node.DisplayName);
                    Assert.False(node.MayHaveChildren);
                    Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
                    Assert.Equal(NodeType.AssemblyReference, node.Metadata?.Type);
                });
    }

    [Fact]
    public async Task GetNamespaceChildren()
    {
        var application = await TestHelper.CreateTestApplication();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "TestAssembly", 0, 0);
        var list = await application.TreeNodeProviders.ForNode(nodeMetadata).GetChildrenAsync(nodeMetadata);
        Assert.Collection(list,
                node => {
                    Assert.Equal("ISomeInterface", node.Metadata?.Name);
                    Assert.Equal("ISomeInterface", node.DisplayName);
                    Assert.Equal(NodeType.Interface, node.Metadata?.Type);
                    Assert.NotEqual(0, node.Metadata?.SymbolToken);
                    Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Abstract, node.SymbolModifiers);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("SomeClass", node.Metadata?.Name);
                    Assert.Equal("SomeClass", node.DisplayName);
                    Assert.Equal(NodeType.Class, node.Metadata?.Type);
                    Assert.NotEqual(0, node.Metadata?.SymbolToken);
                    Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("SomeDelegate", node.Metadata?.Name);
                    Assert.Equal("SomeDelegate", node.DisplayName);
                    Assert.Equal(NodeType.Delegate, node.Metadata?.Type);
                    Assert.NotEqual(0, node.Metadata?.SymbolToken);
                    Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Sealed, node.SymbolModifiers);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("SomeEnum", node.Metadata?.Name);
                    Assert.Equal("SomeEnum", node.DisplayName);
                    Assert.Equal(NodeType.Enum, node.Metadata?.Type);
                    Assert.NotEqual(0, node.Metadata?.SymbolToken);
                    Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Sealed, node.SymbolModifiers);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("SomeStruct", node.Metadata?.Name);
                    Assert.Equal("SomeStruct", node.DisplayName);
                    Assert.Equal(NodeType.Struct, node.Metadata?.Type);
                    Assert.NotEqual(0, node.Metadata?.SymbolToken);
                    Assert.Equal(SymbolModifiers.Internal | SymbolModifiers.Sealed, node.SymbolModifiers);
                    Assert.True(node.MayHaveChildren);
                });
    }

    private static TokenType GetTokenTypeFromToken(int handle)
    {
        return (TokenType) (handle & (0xFF << 24));
    }

    [Fact]
    public async Task GetTypeChildren()
    {
        var application = await TestHelper.CreateTestApplication();
        var types = await application.TreeNodeProviders.Namespace.GetChildrenAsync(
            new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "TestAssembly", 0, 0));
        var typeNode = types.Where(node => node.Metadata?.Name == "SomeClass").First();
        var list = await application.TreeNodeProviders.ForNode(typeNode.Metadata).GetChildrenAsync(typeNode.Metadata);
        Assert.Collection(list,
                node => {
                    Assert.Equal("NestedC", node.Metadata?.Name);
                    Assert.Equal(NodeType.Class, node.Metadata?.Type);
                    Assert.NotEqual(0, node.Metadata?.SymbolToken);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                    Assert.True(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("_ProgId", node.Metadata?.Name);
                    Assert.Equal(NodeType.Field, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Private, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("ProgId", node.Metadata?.Name);
                    Assert.Equal(NodeType.Property, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("op_BitwiseAnd(SomeClass, SomeClass) : SomeClass", node.Metadata?.Name);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Static, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("SomeClass()", node.Metadata?.Name);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Private | SymbolModifiers.Static, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("SomeClass()", node.Metadata?.Name);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("SomeClass(int)", node.Metadata?.Name);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Internal, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("ToString() : string", node.Metadata?.Name);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Override, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                },
                node => {
                    Assert.Equal("VirtualMethod() : void", node.Metadata?.Name);
                    Assert.Equal(NodeType.Method, node.Metadata?.Type);
                    Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                    Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Virtual, node.SymbolModifiers);
                    Assert.False(node.MayHaveChildren);
                }
            );
    }
}

