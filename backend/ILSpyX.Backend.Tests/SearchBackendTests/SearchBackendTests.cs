using ILSpyX.Backend.Model;
using ILSpyX.Backend.Search;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;

namespace ILSpyX.Backend.Tests.SearchBackendTests;

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

    [Fact]
    public async Task LiteralWithPrefix()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("c:Test2", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("SomeClass.CallsFrameworkMethod() : string", node.DisplayName);
        Assert.Equal("TestAssembly.SomeClass", node.Description);
        Assert.Equal(NodeType.Method, node.Metadata?.Type);
        Assert.NotEqual(0, node.Metadata?.SymbolToken);
    }

    [Fact]
    public async Task LiteralWithoutPrefixIsNotFound()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("Test2", CancellationToken.None);
        Assert.Empty(nodeData);
    }

    [Fact]
    public async Task TypeWithPrefix()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("t:SomeClass", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("SomeClass", node.DisplayName);
        Assert.Equal(NodeType.Class, node.Metadata?.Type);
    }

    [Fact]
    public async Task NamespaceWithPrefix()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("n:Generics", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("Generics", node.DisplayName);
        Assert.Equal("TestAssembly", node.Description);
        Assert.Equal(NodeType.Namespace, node.Metadata?.Type);
        Assert.Equal("Generics", node.Metadata?.Name);
        Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
    }

    [Fact]
    public async Task AssemblyWithPrefix()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("a:TestAssembly", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal(NodeType.Assembly, node.Metadata?.Type);
        Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
        Assert.StartsWith("TestAssembly", node.DisplayName);
    }

    [Fact]
    public async Task AssemblyInNuGetPackageWithPrefix()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var searchBackend = services.GetRequiredService<SearchBackend>();
        var nodeData = await searchBackend.Search("a:TestAssembly", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal(NodeType.Assembly, node.Metadata?.Type);
        Assert.Equal(TestHelper.NuGetPackagePath, node.Metadata?.AssemblyPath);
        Assert.Equal(TestHelper.NuGetBundledAssemblyName, node.Metadata?.BundledAssemblyName);
    }

    [Fact]
    public async Task TypeWithPrefixInNamespace()
    {
        var searchBackend = await CreateSearchBackend();
        Assert.Single(await searchBackend.Search("t:SomeClass innamespace:TestAssembly", CancellationToken.None));
        Assert.Empty(await searchBackend.Search("t:SomeClass innamespace:Generics", CancellationToken.None));
    }

    [Fact]
    public async Task AllFindsLiterals()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("all:Test2", CancellationToken.None);
        var node = Assert.Single(nodeData);
        Assert.Equal("SomeClass.CallsFrameworkMethod() : string", node.DisplayName);
        Assert.Equal(NodeType.Method, node.Metadata?.Type);
    }

    [Fact]
    public async Task AllFindsTypesNamespacesAndAssemblies()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = (await searchBackend.Search("all:Generics", CancellationToken.None)).ToList();
        Assert.Contains(nodeData, node => node.Metadata?.Type == NodeType.Namespace && node.DisplayName == "Generics");

        nodeData = (await searchBackend.Search("all:SomeClass", CancellationToken.None)).ToList();
        Assert.Contains(nodeData, node => node.Metadata?.Type == NodeType.Class && node.DisplayName == "SomeClass");

        nodeData = (await searchBackend.Search("all:TestAssembly", CancellationToken.None)).ToList();
        Assert.Contains(nodeData, node => node.Metadata?.Type == NodeType.Assembly);
        Assert.Contains(nodeData, node => node.Metadata?.Type == NodeType.Namespace);
    }

    [Fact]
    public async Task AllHasNoDuplicates()
    {
        var searchBackend = await CreateSearchBackend();
        foreach (string term in new[] { "all:Test", "all:Some", "all:a" })
        {
            var nodeData = (await searchBackend.Search(term, CancellationToken.None)).ToList();
            Assert.Equal(nodeData.Count, nodeData.Select(node => node.Metadata).Distinct().Count());
        }
    }

    [Fact]
    public async Task AllIsCaseInsensitivePrefix()
    {
        var searchBackend = await CreateSearchBackend();
        Assert.Single(await searchBackend.Search("ALL:Test2", CancellationToken.None));
    }
}
