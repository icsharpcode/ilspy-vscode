using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata.Ecma335;

namespace ILSpyX.Backend.Tests.DecompilerBackendTests;

public class NodePositionTests
{
    [Fact]
    public async Task TypeNodePositionsPointIntoDecompiledCode()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var assemblyFile = new AssemblyFileIdentifier(TestHelper.AssemblyPath);
        var typeToken = await TestHelper.GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        Assert.NotEqual(0, typeToken);

        var code = (await decompilerBackend.GetCode(assemblyFile, MetadataTokens.EntityHandle(typeToken),
            LanguageName.CSharpLatest)).DecompiledCode;
        Assert.NotNull(code);
        var lines = code!.Split('\n');

        var positions = (await decompilerBackend.GetNodePositions(assemblyFile,
            MetadataTokens.EntityHandle(typeToken), LanguageName.CSharpLatest)).ToList();

        var fieldToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "_ProgId");
        var propertyToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "ProgId");
        var methodToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "VirtualMethod() : void");
        var nestedTypeToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "NestedC");
        Assert.NotEqual(0, fieldToken);
        Assert.NotEqual(0, propertyToken);
        Assert.NotEqual(0, methodToken);
        Assert.NotEqual(0, nestedTypeToken);

        var positionsByToken = positions.ToDictionary(p => p.SymbolToken);
        foreach (var (token, name) in new[]
                 {
                     (fieldToken, "_ProgId"),
                     (propertyToken, "ProgId"),
                     (methodToken, "VirtualMethod"),
                     (nestedTypeToken, "NestedC"),
                 })
        {
            Assert.True(positionsByToken.TryGetValue(token, out var position),
                $"No position returned for member '{name}'");
            Assert.True(position.StartLine < lines.Length, $"Start line out of range for '{name}'");
            Assert.Contains(name, lines[position.StartLine]);
            Assert.True(position.EndLine >= position.StartLine, $"Invalid range for '{name}'");
        }
    }

    [Fact]
    public async Task MembersHiddenInDecompiledCodeFallBackToTypeDeclaration()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var assemblyFile = new AssemblyFileIdentifier(TestHelper.AssemblyPath);
        // A1 has only an implicit default constructor, which has no declaration in the decompiled code.
        var typeToken = await TestHelper.GetTypeToken(decompilerBackend, "A", "A1");
        var ctorToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "A1()");
        Assert.NotEqual(0, ctorToken);

        var code = (await decompilerBackend.GetCode(assemblyFile, MetadataTokens.EntityHandle(typeToken),
            LanguageName.CSharpLatest)).DecompiledCode;
        var lines = code!.Split('\n');

        var position = Assert.Single(await decompilerBackend.GetNodePositions(assemblyFile,
            MetadataTokens.EntityHandle(typeToken), LanguageName.CSharpLatest),
            p => p.SymbolToken == ctorToken);
        Assert.Contains("class A1", lines[position.StartLine]);
    }

    [Fact]
    public async Task NonTypeNodeReturnsSelfPositionAtDocumentStart()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var assemblyFile = new AssemblyFileIdentifier(TestHelper.AssemblyPath);
        var typeToken = await TestHelper.GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        var methodToken = await TestHelper.GetMemberToken(decompilerBackend, typeToken, "VirtualMethod() : void");
        Assert.NotEqual(0, methodToken);

        var memberPosition = Assert.Single(await decompilerBackend.GetNodePositions(assemblyFile,
            MetadataTokens.EntityHandle(methodToken), LanguageName.CSharpLatest));
        Assert.Equal(methodToken, memberPosition.SymbolToken);
        Assert.Equal(0, memberPosition.StartLine);
        Assert.Equal(0, memberPosition.StartCharacter);

        var assemblyPosition = Assert.Single(await decompilerBackend.GetNodePositions(assemblyFile,
            MetadataTokens.EntityHandle(0x20000001), LanguageName.CSharpLatest));
        Assert.Equal(0x20000001, assemblyPosition.SymbolToken);
        Assert.Equal(0, assemblyPosition.StartLine);
    }

    [Fact]
    public async Task TypeNodeInILHasNoPositions()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var assemblyFile = new AssemblyFileIdentifier(TestHelper.AssemblyPath);
        var typeToken = await TestHelper.GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");

        Assert.Empty(await decompilerBackend.GetNodePositions(assemblyFile,
            MetadataTokens.EntityHandle(typeToken), LanguageName.IL));
    }
}
