using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;

namespace ILSpyX.Backend.Tests;

public class TreeNodeProviderTests
{
    [Fact]
    public async Task GetRootNodes()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var list = await services.GetRequiredService<AssemblyTreeRootNodesProvider>().GetChildrenAsync(null);
        var node = Assert.Single(list);
        Assert.Equal("TestAssembly, 1.0.0.0, .NETCoreApp, v8.0", node.DisplayName);
        Assert.Equal(TestHelper.AssemblyPath, node.Description);
        Assert.True(node.MayHaveChildren);
        Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
        Assert.Equal(Path.GetFileName(TestHelper.AssemblyPath), node.Metadata?.Name);
        Assert.Equal(NodeType.Assembly, node.Metadata?.Type);
    }

    [Fact]
    public async Task GetAssemblyChildren()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath, Type = NodeType.Assembly, Name = TestHelper.AssemblyPath,
        };
        var list = await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
            .GetChildrenAsync(nodeMetadata);
        
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
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath, Type = NodeType.ReferencesRoot, Name = "References",
        };
        var list = await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
            .GetChildrenAsync(nodeMetadata);
        var node = Assert.Single(list);
        Assert.StartsWith("System.Runtime", node.Metadata?.Name);
        Assert.StartsWith("System.Runtime, Version=", node.DisplayName);
        Assert.False(node.MayHaveChildren);
        Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
        Assert.Equal(NodeType.AssemblyReference, node.Metadata?.Type);
    }

    [Fact]
    public async Task GetNamespaceChildren()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath, Type = NodeType.Namespace, Name = "TestAssembly",
        };
        var list = await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
            .GetChildrenAsync(nodeMetadata);
        
        Assert.Collection(list,
            node => {
                Assert.Equal("IDerivedInterface", node.Metadata?.Name);
                Assert.Equal("IDerivedInterface", node.DisplayName);
                Assert.Equal(NodeType.Interface, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Abstract, node.SymbolModifiers);
                Assert.True(node.MayHaveChildren);
            }, node => {
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
                Assert.Equal("SomeInterfaceImplementor", node.Metadata?.Name);
                Assert.Equal("SomeInterfaceImplementor", node.DisplayName);
                Assert.Equal(NodeType.Class, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                Assert.True(node.MayHaveChildren);
            },
            node => {
                Assert.Equal("SomeStruct", node.Metadata?.Name);
                Assert.Equal("SomeStruct", node.DisplayName);
                Assert.Equal(NodeType.Struct, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.Internal | SymbolModifiers.Sealed, node.SymbolModifiers);
                Assert.True(node.MayHaveChildren);
            }
        );
    }

    private static TokenType GetTokenTypeFromToken(int handle)
    {
        return (TokenType) (handle & (0xFF << 24));
    }

    [Fact]
    public async Task GetTypeChildren()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var types = await services.GetRequiredService<NamespaceNodeProvider>().GetChildrenAsync(
            new NodeMetadata
            {
                AssemblyPath = TestHelper.AssemblyPath, Type = NodeType.Namespace, Name = "TestAssembly",
            });
        var typeNode = types.First(node => node.Metadata?.Name == "SomeClass");
        var list = await services.GetRequiredService<TreeNodeProviders>().ForNode(typeNode.Metadata)
            .GetChildrenAsync(typeNode.Metadata);
        
        Assert.Collection(list,
            node => {
                Assert.Equal("Base Types", node.Metadata?.Name);
                Assert.Equal(NodeType.BaseTypes, node.Metadata?.Type);
                Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.None, node.SymbolModifiers);
                Assert.True(node.MayHaveChildren);
            },
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
                Assert.Equal("CallsFrameworkMethod() : string", node.Metadata?.Name);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
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
                Assert.Equal("SomeCompilerSpecial() : string", node.Metadata?.Name);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
                Assert.Equal(typeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                Assert.False(node.MayHaveChildren);
                Assert.Equal(NodeFlags.CompilerGenerated, node.Flags);
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

    [Fact]
    public async Task GetBaseTypes()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var types = await services.GetRequiredService<NamespaceNodeProvider>().GetChildrenAsync(
            new NodeMetadata
            {
                AssemblyPath = TestHelper.AssemblyPath,
                Type = NodeType.Namespace,
                Name = "TestAssembly",
                SymbolToken = 0,
                ParentSymbolToken = 0
            });
        var typeNode = types.First(node => node.Metadata?.Name == "SomeInterfaceImplementor");
        var typesList = await services.GetRequiredService<TreeNodeProviders>().ForNode(typeNode.Metadata)
            .GetChildrenAsync(typeNode.Metadata);
        var baseTypesNode = typesList.First(node => node.Metadata?.Type == NodeType.BaseTypes);
        var baseTypesList = await services.GetRequiredService<TreeNodeProviders>().ForNode(baseTypesNode.Metadata)
            .GetChildrenAsync(baseTypesNode.Metadata);

        Assert.Collection(baseTypesList,
            node => {
                Assert.Equal("ISomeInterface", node.Metadata?.Name);
                Assert.Equal("TestAssembly.ISomeInterface", node.DisplayName);
                Assert.Equal(NodeType.Interface, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Abstract, node.SymbolModifiers);
                Assert.True(node.Metadata?.IsDecompilable);
                Assert.False(node.MayHaveChildren);
            },
            node => {
                Assert.Equal("Object", node.Metadata?.Name);
                Assert.Equal("System.Object", node.DisplayName);
                Assert.Equal(NodeType.Class, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                Assert.True(node.Metadata?.IsDecompilable);
                Assert.False(node.MayHaveChildren);
            }
        );

        // Decompilation test verifies validity of NodeMetadata
        var iSomeInterfaceMetadata = baseTypesList.ElementAt(0).Metadata;
        string? decompiledCode = (await services.GetRequiredService<TreeNodeProviders>().ForNode(iSomeInterfaceMetadata)
            .Decompile(iSomeInterfaceMetadata!, LanguageName.CSharpLatest)).DecompiledCode;
        Assert.Contains("public interface ISomeInterface", decompiledCode);

        var systemObjectMetadata = baseTypesList.ElementAt(1).Metadata;
        decompiledCode = (await services.GetRequiredService<TreeNodeProviders>().ForNode(systemObjectMetadata)
            .Decompile(systemObjectMetadata!, LanguageName.CSharpLatest)).DecompiledCode;
        Assert.Contains("public class Object", decompiledCode);
    }

    [Fact]
    public async Task GetDerivedTypes()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var types = await services.GetRequiredService<NamespaceNodeProvider>().GetChildrenAsync(
            new NodeMetadata
            {
                AssemblyPath = TestHelper.AssemblyPath,
                Type = NodeType.Namespace,
                Name = "TestAssembly",
                SymbolToken = 0,
                ParentSymbolToken = 0
            });
        var typeNode = types.First(node => node.Metadata?.Name == "ISomeInterface");
        var typesList = await services.GetRequiredService<TreeNodeProviders>().ForNode(typeNode.Metadata)
            .GetChildrenAsync(typeNode.Metadata);
        var derivedTypesNode = typesList.First(node => node.Metadata?.Type == NodeType.DerivedTypes);
        var derivedTypesList = await services.GetRequiredService<TreeNodeProviders>().ForNode(derivedTypesNode.Metadata)
            .GetChildrenAsync(derivedTypesNode.Metadata);

        Assert.Collection(derivedTypesList,
            node => {
                Assert.Equal("SomeInterfaceImplementor", node.Metadata?.Name);
                Assert.Equal("TestAssembly.SomeInterfaceImplementor", node.DisplayName);
                Assert.Equal(NodeType.Class, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                Assert.True(node.Metadata?.IsDecompilable);
                Assert.False(node.MayHaveChildren);
            },
            node => {
                Assert.Equal("IDerivedInterface", node.Metadata?.Name);
                Assert.Equal("TestAssembly.IDerivedInterface", node.DisplayName);
                Assert.Equal(NodeType.Interface, node.Metadata?.Type);
                Assert.NotEqual(0, node.Metadata?.SymbolToken);
                Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Abstract, node.SymbolModifiers);
                Assert.True(node.Metadata?.IsDecompilable);
                Assert.False(node.MayHaveChildren);
            }
        );

        // Decompilation test verifies validity of NodeMetadata
        var someInterfaceImplementorMetadata = derivedTypesList.ElementAt(0).Metadata;
        string? decompiledCode = (await services.GetRequiredService<TreeNodeProviders>()
            .ForNode(someInterfaceImplementorMetadata)
            .Decompile(someInterfaceImplementorMetadata!, LanguageName.CSharpLatest)).DecompiledCode;
        Assert.Contains("public class SomeInterfaceImplementor", decompiledCode);

        var iDerivedInterfaceMetadata = derivedTypesList.ElementAt(1).Metadata;
        decompiledCode = (await services.GetRequiredService<TreeNodeProviders>().ForNode(iDerivedInterfaceMetadata)
            .Decompile(iDerivedInterfaceMetadata!, LanguageName.CSharpLatest)).DecompiledCode;
        Assert.Contains("public interface IDerivedInterface", decompiledCode);
    }
}