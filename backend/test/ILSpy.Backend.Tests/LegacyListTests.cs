using ILSpy.Backend.Decompiler;
using Microsoft.Extensions.Logging.Abstractions;
using Mono.Cecil;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace ILSpy.Backend.Tests;

public class LegacyListTests
{
    private static string AssemblyPath => Path.Combine(Path.GetDirectoryName(typeof(DecompileNodeTests).Assembly.Location) ?? "", "TestAssembly.dll");

    private static DecompilerBackend CreateDecompilerBackend()
    {
        var decompilerBackend = new DecompilerBackend(new NullLoggerFactory(), new ILSpySettings());
        decompilerBackend.AddAssembly(AssemblyPath);

        return decompilerBackend;
    }

    [Fact]
    public void ListNamespaces()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var list = decompilerBackend.ListNamespaces(AssemblyPath);
        Assert.Collection(list,
                @namespace => Assert.Equal("", @namespace),
                @namespace => Assert.Equal("A", @namespace),
                @namespace => Assert.Equal("A.B", @namespace),
                @namespace => Assert.Equal("A.B.C", @namespace),
                @namespace => Assert.Equal("A.B.C.D", @namespace),
                @namespace => Assert.Equal("Generics", @namespace),
                @namespace => Assert.Equal("TestAssembly", @namespace)
            );
    }

    [Fact]
    public void ListTypes()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var list = decompilerBackend.ListTypes(AssemblyPath, "TestAssembly");
        Assert.Collection(list,
                memberData => {
                    Assert.Equal("ISomeInterface", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.Interface, memberData.SubKind);
                    Assert.NotEqual(0, memberData.Token);
                },
                memberData => {
                    Assert.Equal("SomeClass", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.Class, memberData.SubKind);
                    Assert.NotEqual(0, memberData.Token);
                },
                memberData => {
                    Assert.Equal("SomeDelegate", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.Delegate, memberData.SubKind);
                    Assert.NotEqual(0, memberData.Token);
                },
                memberData => {
                    Assert.Equal("SomeEnum", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.Enum, memberData.SubKind);
                    Assert.NotEqual(0, memberData.Token);
                },
                memberData => {
                    Assert.Equal("SomeStruct", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.Struct, memberData.SubKind);
                    Assert.NotEqual(0, memberData.Token);
                }
            );
    }

    private static TokenType GetTokenTypeFromToken(int handle)
    {
        return (TokenType) (handle & (0xFF << 24));
    }

    [Fact]
    public void ListMembers()
    {
        var decompilerBackend = CreateDecompilerBackend();
        var types = decompilerBackend.ListTypes(AssemblyPath, "TestAssembly");
        var members = decompilerBackend.GetMembers(
            AssemblyPath,
            MetadataTokens.TypeDefinitionHandle(types.Where(type => type.Name == "SomeClass").First().Token));
        Assert.Collection(members,
                memberData => {
                    Assert.Equal("NestedC", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.Class, memberData.SubKind);
                    Assert.NotEqual(0, memberData.Token);
                },
                memberData => {
                    Assert.Equal("_ProgId", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Field, GetTokenTypeFromToken(memberData.Token));
                },
                memberData => {
                    Assert.Equal("ProgId", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Property, GetTokenTypeFromToken(memberData.Token));
                },
                memberData => {
                    Assert.Equal("op_BitwiseAnd(SomeClass, SomeClass) : SomeClass", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Method, GetTokenTypeFromToken(memberData.Token));
                },
                memberData => {
                    Assert.Equal("SomeClass()", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Method, GetTokenTypeFromToken(memberData.Token));
                },
                memberData => {
                    Assert.Equal("SomeClass()", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Method, GetTokenTypeFromToken(memberData.Token));
                },
                memberData => {
                    Assert.Equal("SomeClass(int)", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Method, GetTokenTypeFromToken(memberData.Token));
                },
                memberData => {
                    Assert.Equal("ToString() : string", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Method, GetTokenTypeFromToken(memberData.Token));
                },
                memberData => {
                    Assert.Equal("VirtualMethod() : void", memberData.Name);
                    Assert.Equal(ICSharpCode.Decompiler.TypeSystem.TypeKind.None, memberData.SubKind);
                    Assert.Equal(TokenType.Method, GetTokenTypeFromToken(memberData.Token));
                }
            );
    }
}

