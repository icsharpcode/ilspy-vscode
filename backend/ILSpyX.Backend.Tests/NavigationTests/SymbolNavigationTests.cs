using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.Navigation;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata.Ecma335;

namespace ILSpyX.Backend.Tests.NavigationTests;

public class SymbolNavigationTests
{
    private record Document(NodeMetadata Metadata, string Code)
    {
        // Zero-based position of the given occurrence of a text, with an optional offset into the text.
        public (int Line, int Column) PositionOf(string text, int occurrence = 0, int offset = 0)
        {
            int index = -1;
            for (int i = 0; i <= occurrence; i++)
            {
                index = Code.IndexOf(text, index + 1, StringComparison.Ordinal);
                Assert.True(index >= 0, $"'{text}' not found in:\n{Code}");
            }

            index += offset;
            int lineStart = Code.LastIndexOf('\n', Math.Max(index - 1, 0)) + 1;
            return (Code[..index].Count(c => c == '\n'), index - lineStart);
        }

        public string TextAt(CodeLocation location) =>
            Code.Split('\n')[location.Line].Substring(location.Column, location.Length);
    }

    private static async Task<Document> Decompile(IServiceProvider services, string typeName,
        string? memberNamePrefix = null, string outputLanguage = LanguageName.CSharpLatest)
    {
        var types = await services.GetRequiredService<NamespaceNodeProvider>().GetChildrenAsync(
            new NodeMetadata
            {
                AssemblyPath = TestHelper.AssemblyPath, Type = NodeType.Namespace, Name = "TestAssembly"
            });
        var node = types.First(node => node.Metadata?.Name == typeName);
        if (memberNamePrefix is not null)
        {
            var members = await services.GetRequiredService<TypeNodeProvider>().GetChildrenAsync(node.Metadata);
            node = members.First(member => member.Metadata?.Name?.StartsWith(memberNamePrefix) ?? false);
        }

        return await Decompile(services, node.Metadata!, outputLanguage);
    }

    private static async Task<Document> Decompile(IServiceProvider services, NodeMetadata metadata,
        string outputLanguage = LanguageName.CSharpLatest)
    {
        var code = (await services.GetRequiredService<TreeNodeProviders>().ForNode(metadata)
            .Decompile(metadata, outputLanguage)).DecompiledCode;
        return new Document(metadata, code!);
    }

    private static Task<IEnumerable<CodeLocation>> FindDefinitions(IServiceProvider services, Document document,
        (int Line, int Column) position, string outputLanguage = LanguageName.CSharpLatest) =>
        services.GetRequiredService<SymbolNavigationBackend>()
            .FindDefinitions(document.Metadata, outputLanguage, position.Line, position.Column);

    private static Task<IEnumerable<CodeLocation>> FindImplementations(IServiceProvider services,
        Document document, (int Line, int Column) position) =>
        services.GetRequiredService<SymbolNavigationBackend>()
            .FindImplementations(document.Metadata, LanguageName.CSharpLatest, position.Line, position.Column);

    [Fact]
    public async Task ReferencesCoverTheirTextInDecompiledCode()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var someClass = await Decompile(services, "SomeClass");
        var document = await services.GetRequiredService<DecompilerBackend>().GetDecompiledDocument(
            someClass.Metadata.GetAssemblyFileIdentifier(),
            MetadataTokens.EntityHandle(someClass.Metadata.SymbolToken), LanguageName.CSharpLatest);

        Assert.NotNull(document);
        Assert.Equal(someClass.Code, document.Code);
        Assert.NotEmpty(document.References);
        var lines = document.Code.Split('\n');
        Assert.All(document.References, reference => {
            string text = lines[reference.Line].Substring(reference.Column, reference.Length);
            Assert.Matches(@"^[@\w<>.~]+$", text);
        });
    }

    [Fact]
    public async Task DefinitionInSameDocument()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var someClass = await Decompile(services, "SomeClass");

        var location = Assert.Single(await FindDefinitions(services, someClass,
            someClass.PositionOf("ProgId = ProgramId", offset: 2)));

        Assert.Null(location.Node);
        Assert.Equal(someClass.PositionOf("int ProgId", offset: 4), (location.Line, location.Column));
        Assert.Equal("ProgId", someClass.TextAt(location));
    }

    [Fact]
    public async Task DefinitionOfMemberInOtherDocument()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var constructor = await Decompile(services, "SomeClass", "SomeClass(int)");

        var location = Assert.Single(await FindDefinitions(services, constructor,
            constructor.PositionOf("ProgId = ProgramId")));

        Assert.NotNull(location.Node?.Metadata);
        Assert.Equal(NodeType.Property, location.Node.Metadata.Type);
        Assert.Equal("ProgId", location.Node.Metadata.Name);
        Assert.Equal(constructor.Metadata.ParentSymbolToken, location.Node.Metadata.ParentSymbolToken);
        var property = await Decompile(services, "SomeClass", "ProgId");
        Assert.Equal(property.Metadata, location.Node.Metadata);
        Assert.Equal("ProgId", property.TextAt(location));
    }

    [Fact]
    public async Task DefinitionOfConstructor()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var structMethod = await Decompile(services, "SomeStruct", "StructMethod");

        var location = Assert.Single(await FindDefinitions(services, structMethod,
            structMethod.PositionOf("new SomeClass", offset: 4)));

        Assert.NotNull(location.Node?.Metadata);
        Assert.Equal(NodeType.Method, location.Node.Metadata.Type);
        Assert.Equal("SomeClass()", location.Node.Metadata.Name);
        // Not the static constructor having the same name
        var constructor = await Decompile(services, location.Node.Metadata);
        Assert.Contains("public SomeClass()", constructor.Code);
        Assert.Equal("SomeClass", constructor.TextAt(location));
    }

    [Fact]
    public async Task DefinitionOfTypeInOtherDocument()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var operatorMethod = await Decompile(services, "SomeClass", "op_BitwiseAnd");
        var someClass = await Decompile(services, "SomeClass");

        var location = Assert.Single(await FindDefinitions(services, operatorMethod,
            operatorMethod.PositionOf("SomeClass a")));

        Assert.NotNull(location.Node?.Metadata);
        Assert.Equal(someClass.Metadata, location.Node.Metadata);
        Assert.Equal(someClass.PositionOf("class SomeClass", offset: 6), (location.Line, location.Column));
    }

    [Fact]
    public async Task DefinitionOfFrameworkType()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var method = await Decompile(services, "SomeClass", "CallsFrameworkMethod");

        var location = Assert.Single(await FindDefinitions(services, method, method.PositionOf("string")));

        Assert.Equal(NodeType.Class, location.Node?.Metadata?.Type);
        Assert.Equal("String", location.Node?.Metadata?.Name);
        Assert.NotEqual(TestHelper.AssemblyPath, location.Node?.Metadata?.AssemblyPath);
    }

    [Fact]
    public async Task DefinitionOfLocalVariable()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var structMethod = await Decompile(services, "SomeStruct", "StructMethod");

        var location = Assert.Single(await FindDefinitions(services, structMethod,
            structMethod.PositionOf("someClass.ToString")));

        Assert.Null(location.Node);
        Assert.Equal(structMethod.PositionOf("someClass = new"), (location.Line, location.Column));
    }

    [Fact]
    public async Task DefinitionOfParameter()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var constructor = await Decompile(services, "SomeClass", "SomeClass(int)");

        var location = Assert.Single(await FindDefinitions(services, constructor,
            constructor.PositionOf("= ProgramId", offset: 2)));

        Assert.Null(location.Node);
        Assert.Equal(constructor.PositionOf("int ProgramId", offset: 4), (location.Line, location.Column));
    }

    [Fact]
    public async Task DefinitionInFrameworkAssembly()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var method = await Decompile(services, "SomeClass", "CallsFrameworkMethod");

        var location = Assert.Single(await FindDefinitions(services, method, method.PositionOf("Join")));

        Assert.NotNull(location.Node?.Metadata);
        Assert.NotEqual(TestHelper.AssemblyPath, location.Node.Metadata.AssemblyPath);
        Assert.Equal(NodeType.Method, location.Node.Metadata.Type);
        Assert.StartsWith("Join(", location.Node.Metadata.Name);
    }

    [Fact]
    public async Task NoDefinitionOutsideOfReferences()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var method = await Decompile(services, "SomeClass", "CallsFrameworkMethod");

        Assert.Empty(await FindDefinitions(services, method, method.PositionOf("\"Test1\"")));
        Assert.Empty(await FindDefinitions(services, method, method.PositionOf("return")));
    }

    [Fact]
    public async Task DefinitionFromILCode()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var constructor = await Decompile(services, "SomeClass", "SomeClass(int)", LanguageName.IL);
        var method = await Decompile(services, "SomeClass", "CallsFrameworkMethod", LanguageName.IL);

        var setter = Assert.Single(await FindDefinitions(services, constructor,
            constructor.PositionOf("set_ProgId"), LanguageName.IL));
        var frameworkMethod = Assert.Single(await FindDefinitions(services, method,
            method.PositionOf("::Join", offset: 2), LanguageName.IL));

        Assert.Equal(NodeType.Property, setter.Node?.Metadata?.Type);
        Assert.Equal("ProgId", setter.Node?.Metadata?.Name);
        Assert.NotEqual(TestHelper.AssemblyPath, frameworkMethod.Node?.Metadata?.AssemblyPath);
        Assert.StartsWith("Join(", frameworkMethod.Node?.Metadata?.Name);
    }

    [Fact]
    public async Task ImplementationsOfInterfaceMember()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var someInterface = await Decompile(services, "ISomeInterface");

        var location = Assert.Single(await FindImplementations(services, someInterface,
            someInterface.PositionOf("int I", offset: 4)));

        Assert.NotNull(location.Node?.Metadata);
        Assert.Equal(NodeType.Property, location.Node.Metadata.Type);
        Assert.Equal("I", location.Node.Metadata.Name);
        var implementor = await Decompile(services, "SomeInterfaceImplementor");
        Assert.Equal(implementor.Metadata.SymbolToken, location.Node.Metadata.ParentSymbolToken);
    }

    [Fact]
    public async Task ImplementationsOfInterface()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var someInterface = await Decompile(services, "ISomeInterface");

        var locations = await FindImplementations(services, someInterface,
            someInterface.PositionOf("ISomeInterface"));

        Assert.Equal(["IDerivedInterface", "SomeInterfaceImplementor"],
            locations.Select(location => location.Node?.Metadata?.Name).Order());
    }

    [Fact]
    public async Task ImplementationsOfVirtualMethodIncludeOverridesInSameDocument()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var someClass = await Decompile(services, "SomeClass");

        var locations = (await FindImplementations(services, someClass,
            someClass.PositionOf("base.ToString", offset: 5))).ToList();

        var overrideInSameDocument = Assert.Single(locations, location => location.Node is null);
        Assert.Equal(someClass.PositionOf("string ToString", offset: 7),
            (overrideInSameDocument.Line, overrideInSameDocument.Column));
        Assert.Contains(locations, location =>
            location.Node?.Metadata?.Name?.StartsWith("ToString") == true &&
            location.Node.Metadata.AssemblyPath != TestHelper.AssemblyPath);
    }
}
