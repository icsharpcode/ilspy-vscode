using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata.Ecma335;

namespace ILSpyX.Backend.Tests.DecompilerBackendTests;

public class NuGetPackageTreeNodeDecompilationTests
{
    private static async Task<int> GetTypeToken(DecompilerBackend decompilerBackend, string @namespace,
        string name)
    {
        return (await decompilerBackend
                .ListTypes(
                    new AssemblyFileIdentifier(TestHelper.NuGetPackagePath, TestHelper.NuGetBundledAssemblyName),
                    @namespace))
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }

    private static async Task<int> GetMemberToken(DecompilerBackend decompilerBackend, int parentTypeToken, string name)
    {
        return (await decompilerBackend
                .GetMembers(
                    new AssemblyFileIdentifier(TestHelper.NuGetPackagePath, TestHelper.NuGetBundledAssemblyName),
                    MetadataTokens.TypeDefinitionHandle(parentTypeToken)))
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }

    [Fact]
    public async Task Assembly()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Assembly,
            Name = TestHelper.AssemblyPath
        };
        Assert.Equal(
            $"// {TestHelper.NuGetBundledAssemblyName}" +
            @"
// TestAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Global type: <Module>
// Architecture: AnyCPU (64-bit preferred)
// Runtime: v4.0.30319

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: TargetFramework("".NETCoreApp,Version=v10.0"", FrameworkDisplayName = "".NET 10.0"")]
[assembly: AssemblyCompany(""TestAssembly"")]
[assembly: AssemblyConfiguration(""Debug"")]
[assembly: AssemblyFileVersion(""1.0.0.0"")]
[assembly: AssemblyProduct(""TestAssembly"")]
[assembly: AssemblyTitle(""TestAssembly"")]
[assembly: AssemblyVersion(""1.0.0.0"")]
[module: RefSafetyRules(11)]

",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Namespace()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Namespace,
            Name = "A.B.C.D"
        };
        Assert.Equal(
            @"namespace A.B.C.D { }",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task GlobalNamespace()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Namespace,
            Name = ""
        };
        Assert.Equal(
            @"namespace <global> { }",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Class()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken =
            await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "Generics", "AClass");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Class,
            Name = "",
            SymbolToken = typeToken
        };
        Assert.Equal(
            @"namespace Generics;

public class AClass
{
    public class NestedClass<T>
    {
    }

    public class NestedClass<T1, T2>
    {
    }

    public void M<T>()
    {
    }

    public void M<T1, T2>()
    {
    }
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Interface()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken = await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly",
            "ISomeInterface");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Interface,
            Name = "",
            SymbolToken = typeToken
        };
        Assert.Equal(
            @"namespace TestAssembly;

public interface ISomeInterface
{
    int I { get; set; }
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Struct()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken =
            await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly",
                "SomeStruct");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Struct,
            Name = "",
            SymbolToken = typeToken
        };
        Assert.Equal(
            @"namespace TestAssembly;

internal struct SomeStruct
{
    public int Prop { get; set; }

    public string StructMethod()
    {
        SomeClass someClass = new SomeClass();
        return someClass.ToString();
    }
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Enum()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken =
            await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly", "SomeEnum");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Enum,
            Name = "",
            SymbolToken = typeToken
        };
        Assert.Equal(
            @"namespace TestAssembly;

public enum SomeEnum
{
    E1,
    E2,
    E3
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Method()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken =
            await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly",
                "SomeClass");
        int memberToken = await GetMemberToken(services.GetRequiredService<DecompilerBackend>(), typeToken,
            "ToString() : string");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Method,
            Name = "",
            SymbolToken = memberToken,
            ParentSymbolToken = typeToken
        };
        Assert.Equal(
            @"public override string ToString()
{
    return base.ToString() ?? string.Empty;
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Field()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken =
            await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly",
                "SomeClass");
        int memberToken =
            await GetMemberToken(services.GetRequiredService<DecompilerBackend>(), typeToken, "_ProgId");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Field,
            Name = "",
            SymbolToken = memberToken,
            ParentSymbolToken = typeToken
        };
        Assert.Equal(
            @"private int _ProgId;
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Property()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken =
            await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly",
                "SomeClass");
        int memberToken =
            await GetMemberToken(services.GetRequiredService<DecompilerBackend>(), typeToken, "ProgId");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Property,
            Name = "",
            SymbolToken = memberToken,
            ParentSymbolToken = typeToken
        };
        Assert.Equal(
            @"public int ProgId
{
    get
    {
        return _ProgId;
    }
    set
    {
        _ProgId = value;
    }
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task Constructor()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken =
            await GetTypeToken(services.GetRequiredService<DecompilerBackend>(), "TestAssembly",
                "SomeClass");
        int memberToken =
            await GetMemberToken(services.GetRequiredService<DecompilerBackend>(), typeToken,
                "SomeClass(int)");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Method,
            Name = "",
            SymbolToken = memberToken,
            ParentSymbolToken = typeToken
        };
        Assert.Equal(
            @"internal SomeClass(int ProgramId)
{
    ProgId = ProgramId;
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task ReferencesRoot()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.ReferencesRoot,
            Name = "References",
        };
        Assert.Equal(
            @"// System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task AssemblyReference()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.AssemblyReference,
            Name = "System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        };
        Assert.Equal(
            @"// System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task CSharpVariant_Latest()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken = await GetTypeToken(services.GetRequiredService<DecompilerBackend>(),
            "CSharpVariants",
            "CSharpVariants");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Interface,
            Name = "",
            SymbolToken = typeToken,
        };
        Assert.Equal(
            @"namespace CSharpVariants;

public class CSharpVariants
{
    public string? nullableMember;
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }

    [Fact]
    public async Task CSharpVariant_8()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken = await GetTypeToken(services.GetRequiredService<DecompilerBackend>(),
            "CSharpVariants",
            "CSharpVariants");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Interface,
            Name = "",
            SymbolToken = typeToken,
        };
        Assert.Equal(
            @"namespace CSharpVariants
{
    public class CSharpVariants
    {
        public string? nullableMember;
    }
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharp_8)).DecompiledCode);
    }

    [Fact]
    public async Task CSharpVariant_1()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        int typeToken = await GetTypeToken(services.GetRequiredService<DecompilerBackend>(),
            "CSharpVariants",
            "CSharpVariants");
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath,
            BundledAssemblyName = TestHelper.NuGetBundledAssemblyName,
            Type = NodeType.Class,
            Name = "",
            SymbolToken = typeToken,
        };
        Assert.Equal(
            @"using System.Runtime.CompilerServices;

namespace CSharpVariants
{
    public class CSharpVariants
    {
        [Nullable(2)]
        public string nullableMember;
    }
}
",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharp_1)).DecompiledCode);
    }
}