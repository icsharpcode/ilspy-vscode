using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using ILSpyX.Backend.Application;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection.Metadata.Ecma335;

namespace ILSpy.Backend.Tests;

public class TreeNodeDecompilationTests
{
    private static string AssemblyPath => Path.Combine(Path.GetDirectoryName(typeof(TreeNodeDecompilationTests).Assembly.Location) ?? "", "TestAssembly.dll");

    private static ILSpyXApplication CreateTestApplication()
    {
        var application = new ILSpyXApplication(new NullLoggerFactory(), new ILSpyBackendSettings());
        application.DecompilerBackend.AddAssembly(AssemblyPath);
        return application;
    }

    private static int GetTypeToken(DecompilerBackend decompilerBackend, string @namespace, string name)
    {
        return decompilerBackend
            .ListTypes(AssemblyPath, @namespace)
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }

    private static int GetMemberToken(DecompilerBackend decompilerBackend, int parentTypeToken, string name)
    {
        return decompilerBackend
            .GetMembers(AssemblyPath, MetadataTokens.TypeDefinitionHandle(parentTypeToken))
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }

    [Fact]
    public void Assembly()
    {
        var application = CreateTestApplication();
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Assembly, AssemblyPath, 0, 0);
        Assert.Equal(
$"// {AssemblyPath}" +
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
    public void Namespace()
    {
        var application = CreateTestApplication();
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Namespace, "A.B.C.D", 0, 0);
        Assert.Equal(
@"namespace A.B.C.D { }",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void GlobalNamespace()
    {
        var application = CreateTestApplication();
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Namespace, "", 0, 0);
        Assert.Equal(
@"namespace <global> { }",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void Class()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "Generics", "AClass");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Class, "", typeToken, 0);
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
    public void Interface()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "ISomeInterface");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Interface, "", typeToken, 0);
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
    public void Struct()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeStruct");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Struct, "", typeToken, 0);
        Assert.Equal(
@"namespace TestAssembly;

internal struct SomeStruct
{
    public int Prop { get; set; }
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void Enum()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeEnum");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Enum, "", typeToken, 0);
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
    public void Method()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "ToString() : string");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Method, "", memberToken, typeToken);
        Assert.Equal(
@"public override string ToString()
{
    return base.ToString();
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void Field()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "_ProgId");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Field, "", memberToken, typeToken);
        Assert.Equal(
@"private int _ProgId;
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void Property()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "ProgId");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Property, "", memberToken, typeToken);
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
    public void Constructor()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(application.DecompilerBackend, typeToken, "SomeClass(int)");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Method, "", memberToken, typeToken);
        Assert.Equal(
@"internal SomeClass(int ProgramId)
{
    ProgId = ProgramId;
}
",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void ReferencesRoot()
    {
        var application = CreateTestApplication();
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.ReferencesRoot, "References", 0, 0);
        Assert.Equal(
@"// System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void AssemblyReference()
    {
        var application = CreateTestApplication();
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.AssemblyReference,
            "System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", 0, 0);
        Assert.Equal(
@"// System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            application.TreeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, LanguageName.CSharpLatest).DecompiledCode);
    }

    [Fact]
    public void CSharpVariant_Latest()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "CSharpVariants", "CSharpVariants");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Interface, "", typeToken, 0);
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
    public void CSharpVariant_8()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "CSharpVariants", "CSharpVariants");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Interface, "", typeToken, 0);
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
    public void CSharpVariant_1()
    {
        var application = CreateTestApplication();
        int typeToken = GetTypeToken(application.DecompilerBackend, "CSharpVariants", "CSharpVariants");
        var nodeMetadata = new NodeMetadata(AssemblyPath, NodeType.Interface, "", typeToken, 0);
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
