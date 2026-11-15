using ILSpyX.Backend;
using ILSpyX.Backend.Decompiler;
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

    [Theory]
    [InlineData(LanguageName.CSharpLatest)]
    [InlineData(LanguageName.IL)]
    public async Task LiteralMatchIsLocatedInDecompiledCode(string outputLanguage)
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var searchBackend = services.GetRequiredService<SearchBackend>();
        var node = Assert.Single(await searchBackend.Search("c:test2", CancellationToken.None, outputLanguage));
        Assert.NotNull(node.Match);
        Assert.Equal("Test2", node.Match.Text);

        // The match must point to the same text in the code shown when opening the result.
        string code = (await services.GetRequiredService<TreeNodeProviders>().ForNode(node.Metadata)
            .Decompile(node.Metadata!, outputLanguage)).DecompiledCode!;
        string line = code.Split('\n')[node.Match.Line].TrimEnd('\r');
        Assert.Equal("Test2", line.Substring(node.Match.Column, node.Match.Length));
    }

    [Fact]
    public async Task AllLocatesLiteralAndNameMatches()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var searchBackend = services.GetRequiredService<SearchBackend>();
        var literalResult = Assert.Single(
            await searchBackend.Search("all:Test2", CancellationToken.None, LanguageName.CSharpLatest));
        Assert.Equal("Test2", literalResult.Match?.Text);

        // Names are located in the declaration, not in using directives.
        var nameResults = (await searchBackend.Search("all:SomeClass", CancellationToken.None,
            LanguageName.CSharpLatest)).ToList();
        Assert.NotEmpty(nameResults);
        foreach (var node in nameResults)
        {
            Assert.NotNull(node.Match);
            Assert.Equal("SomeClass", node.Match.Text);
            string line = await GetLine(services, node, LanguageName.CSharpLatest);
            Assert.DoesNotContain("using ", line);
            Assert.Equal("SomeClass", line.Substring(node.Match.Column, node.Match.Length));
        }
    }

    [Fact]
    public async Task QualifiedNameMatchIsLocatedByLastPart()
    {
        var searchBackend = await CreateSearchBackend();
        var node = Assert.Single(
            await searchBackend.Search("SomeClass.ToString", CancellationToken.None, LanguageName.CSharpLatest));
        Assert.Equal("ToString", node.Match?.Text);
    }

    [Fact]
    public async Task NoMatchLocationForMetadataTokenSearch()
    {
        var searchBackend = await CreateSearchBackend();
        var node = (await searchBackend.Search("SomeClass", CancellationToken.None)).First();
        var tokenResults = await searchBackend.Search($"@{node.Metadata!.SymbolToken:x8}", CancellationToken.None,
            LanguageName.CSharpLatest);
        Assert.NotEmpty(tokenResults);
        Assert.All(tokenResults, result => Assert.Null(result.Match));
    }

    private static async Task<string> GetLine(IServiceProvider services, SearchResultNode node, string language)
    {
        string code = (await services.GetRequiredService<TreeNodeProviders>().ForNode(node.Metadata)
            .Decompile(node.Metadata!, language)).DecompiledCode!;
        return code.Split('\n')[node.Match!.Line].TrimEnd('\r');
    }

    [Fact]
    public async Task NoMatchLocationWithoutOutputLanguage()
    {
        var searchBackend = await CreateSearchBackend();
        var node = Assert.Single(await searchBackend.Search("c:Test2", CancellationToken.None));
        Assert.Null(node.Match);
    }

    [Fact]
    public async Task NumberLiteralFindsNumbersAndStrings()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("c:401", CancellationToken.None, LanguageName.CSharpLatest);
        Assert.Equal(
            ["StatusCodes.OtherText() : string", "StatusCodes.UnauthorizedCode() : int",
                "StatusCodes.UnauthorizedText() : string"],
            nodeData.Select(node => node.DisplayName).Order());
        Assert.All(nodeData, node => Assert.Contains("401", node.Match?.Text));
    }

    [Fact]
    public async Task AllFindsNumberInStrings()
    {
        var searchBackend = await CreateSearchBackend();
        var nodeData = await searchBackend.Search("all:401", CancellationToken.None);
        Assert.Contains(nodeData, node => node.DisplayName == "StatusCodes.UnauthorizedText() : string");
        Assert.Contains(nodeData, node => node.DisplayName == "StatusCodes.UnauthorizedCode() : int");
    }

    [Fact]
    public async Task QuotedNumberLiteralFindsExactStringOnly()
    {
        var searchBackend = await CreateSearchBackend();
        var node = Assert.Single(await searchBackend.Search("c:\"401\"", CancellationToken.None));
        Assert.Equal("StatusCodes.UnauthorizedText() : string", node.DisplayName);
    }

    [Fact]
    public async Task SearchInDependenciesButNotInFramework()
    {
        // ILSpyX.Backend.dll and its dependencies (like ICSharpCode.ILSpyX.dll) are in the test output folder.
        string assemblyPath = Path.Combine(AppContext.BaseDirectory, "ILSpyX.Backend.dll");
        var services = new ILSpyXBackendServices();
        await services.GetRequiredService<DecompilerBackend>().AddAssemblyAsync(assemblyPath);
        var searchBackend = services.GetRequiredService<SearchBackend>();

        var dependencyResult = Assert.Single(
            await searchBackend.Search("t:LiteralSearchStrategy", CancellationToken.None));
        Assert.Equal("ICSharpCode.ILSpyX.Search", dependencyResult.Description);
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "ICSharpCode.ILSpyX.dll"),
            dependencyResult.Metadata?.AssemblyPath);

        Assert.Empty(await searchBackend.Search("t:ConcurrentQueue", CancellationToken.None));
    }
}
