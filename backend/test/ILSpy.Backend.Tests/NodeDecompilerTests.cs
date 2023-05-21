using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection.Metadata.Ecma335;

namespace ILSpy.Backend.Tests;

public class NodeDecompilerTests
{
    private static string AssemblyPath => Path.Combine(Path.GetDirectoryName(typeof(NodeDecompilerTests).Assembly.Location) ?? "", "TestAssembly.dll");

    private static DecompilerBackend CreateDecompilerBackend()
    {
        var decompilerBackend = new DecompilerBackend(new NullLoggerFactory(), new ILSpyBackendSettings());
        decompilerBackend.AddAssembly(AssemblyPath);

        return decompilerBackend;
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
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        var node = new NodeMetadata(AssemblyPath, NodeType.Assembly, AssemblyPath, 0, 0);
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
[assembly: TargetFramework("".NETCoreApp,Version=v6.0"", FrameworkDisplayName = "".NET 6.0"")]
[assembly: AssemblyCompany(""TestAssembly"")]
[assembly: AssemblyConfiguration(""Debug"")]
[assembly: AssemblyFileVersion(""1.0.0.0"")]
[assembly: AssemblyInformationalVersion(""1.0.0"")]
[assembly: AssemblyProduct(""TestAssembly"")]
[assembly: AssemblyTitle(""TestAssembly"")]
[assembly: AssemblyVersion(""1.0.0.0"")]

",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Namespace()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        var node = new NodeMetadata(AssemblyPath, NodeType.Namespace, "A.B.C.D", 0, 0);
        Assert.Equal(
@"namespace A.B.C.D { }",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void GlobalNamespace()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        var node = new NodeMetadata(AssemblyPath, NodeType.Namespace, "", 0, 0);
        Assert.Equal(
@"namespace <global> { }",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Class()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "Generics", "AClass");
        var node = new NodeMetadata(AssemblyPath, NodeType.Class, "", typeToken, 0);
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
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Interface()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "TestAssembly", "ISomeInterface");
        var node = new NodeMetadata(AssemblyPath, NodeType.Interface, "", typeToken, 0);
        Assert.Equal(
@"namespace TestAssembly;

public interface ISomeInterface
{
    int i { get; set; }
}
",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Struct()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "TestAssembly", "SomeStruct");
        var node = new NodeMetadata(AssemblyPath, NodeType.Struct, "", typeToken, 0);
        Assert.Equal(
@"namespace TestAssembly;

internal struct SomeStruct
{
    public int Prop { get; set; }
}
",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Enum()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "TestAssembly", "SomeEnum");
        var node = new NodeMetadata(AssemblyPath, NodeType.Enum, "", typeToken, 0);
        Assert.Equal(
@"namespace TestAssembly;

public enum SomeEnum
{
    E1,
    E2,
    E3
}
",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Method()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(decompilerBackend, typeToken, "ToString() : string");
        var node = new NodeMetadata(AssemblyPath, NodeType.Method, "", memberToken, typeToken);
        Assert.Equal(
@"public override string ToString()
{
    return base.ToString();
}
",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Field()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(decompilerBackend, typeToken, "_ProgId");
        var node = new NodeMetadata(AssemblyPath, NodeType.Field, "", memberToken, typeToken);
        Assert.Equal(
@"private int _ProgId;
",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Property()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(decompilerBackend, typeToken, "ProgId");
        var node = new NodeMetadata(AssemblyPath, NodeType.Property, "", memberToken, typeToken);
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
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void Constructor()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        int typeToken = GetTypeToken(decompilerBackend, "TestAssembly", "SomeClass");
        int memberToken = GetMemberToken(decompilerBackend, typeToken, "SomeClass(int)");
        var node = new NodeMetadata(AssemblyPath, NodeType.Method, "", memberToken, typeToken);
        Assert.Equal(
@"internal SomeClass(int ProgramId)
{
    ProgId = ProgramId;
}
",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void ReferencesRoot()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        var node = new NodeMetadata(AssemblyPath, NodeType.ReferencesRoot, "References", 0, 0);
        Assert.Equal(
@"// System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }

    [Fact]
    public void AssemblyReference()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var nodeDecompiler = new NodeDecompiler(decompilerBackend);
        var node = new NodeMetadata(AssemblyPath, NodeType.AssemblyReference,
            "System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", 0, 0);
        Assert.Equal(
@"// System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            nodeDecompiler.GetCode(node)?[LanguageNames.CSharp]);
    }
}
