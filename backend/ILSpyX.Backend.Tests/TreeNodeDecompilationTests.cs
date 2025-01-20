using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Reflection.Metadata.Ecma335;

namespace ILSpyX.Backend.Tests;

public class TreeNodeDecompilationTests
{
    private static int GetTypeToken(DecompilerBackend decompilerBackend, string @namespace, string name)
    {
        return decompilerBackend
            .ListTypes(TestHelper.AssemblyPath, @namespace)
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }

    private static int GetMemberToken(DecompilerBackend decompilerBackend, int parentTypeToken, string name)
    {
        return decompilerBackend
            .GetMembers(TestHelper.AssemblyPath, MetadataTokens.TypeDefinitionHandle(parentTypeToken))
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }

    [Fact]
    public async Task Assembly()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Assembly, TestHelper.AssemblyPath, 0, 0);
        Assert.Equal(
$"// {TestHelper.AssemblyPath}" +
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
[assembly: TargetFramework("".NETCoreApp,Version=v8.0"", FrameworkDisplayName = "".NET 8.0"")]
[assembly: AssemblyCompany(""TestAssembly"")]
[assembly: AssemblyConfiguration(""Debug"")]
[assembly: AssemblyFileVersion(""1.0.0.0"")]
[assembly: AssemblyProduct(""TestAssembly"")]
[assembly: AssemblyTitle(""TestAssembly"")]
[assembly: AssemblyVersion(""1.0.0.0"")]
[module: RefSafetyRules(11)]

",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Namespace()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "A.B.C.D", 0, 0);
        Assert.Equal(
@"namespace A.B.C.D { }",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task GlobalNamespace()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "", 0, 0);
        Assert.Equal(
@"namespace <global> { }",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Class()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "Generics", "AClass");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Class, "", typeToken, 0);
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
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Interface()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "ISomeInterface");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Interface, "", typeToken, 0);
        Assert.Equal(
@"namespace TestAssembly;

public interface ISomeInterface
{
    int i { get; set; }
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Struct()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeStruct");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Struct, "", typeToken, 0);
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
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Enum()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeEnum");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Enum, "", typeToken, 0);
        Assert.Equal(
@"namespace TestAssembly;

public enum SomeEnum
{
    E1,
    E2,
    E3
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Method()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "ToString() : string");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Method, "", memberToken, typeToken);
        Assert.Equal(
@"public override string ToString()
{
    return base.ToString();
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Field()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "_ProgId");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Field, "", memberToken, typeToken);
        Assert.Equal(
@"private int _ProgId;
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Property()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "ProgId");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Property, "", memberToken, typeToken);
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
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task Constructor()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "SomeClass(int)");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Method, "", memberToken, typeToken);
        Assert.Equal(
@"internal SomeClass(int ProgramId)
{
    ProgId = ProgramId;
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task ReferencesRoot()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.ReferencesRoot, "References", 0, 0);
        Assert.Equal(
@"// System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task AssemblyReference()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.AssemblyReference,
            "System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", 0, 0);
        Assert.Equal(
@"// System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task CSharpVariant_Latest()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "CSharpVariants", "CSharpVariants");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Interface, "", typeToken, 0);
        Assert.Equal(
@"namespace CSharpVariants;

public class CSharpVariants
{
    public string? nullableMember;
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public async Task CSharpVariant_8()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "CSharpVariants", "CSharpVariants");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Interface, "", typeToken, 0);
        Assert.Equal(
@"namespace CSharpVariants
{
    public class CSharpVariants
    {
        public string? nullableMember;
    }
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharp_8).DecompiledCode);
    }

    [Fact]
    public async Task CSharpVariant_1()
    {
        var application = await TestHelper.CreateTestApplicationWithAssembly();
        int typeToken = GetTypeToken(application.DecompilerBackend, "CSharpVariants", "CSharpVariants");
        var nodeMetadata = new NodeMetadata(TestHelper.AssemblyPath, NodeType.Class, "", typeToken, 0);
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
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharp_1).DecompiledCode);
    }
}
