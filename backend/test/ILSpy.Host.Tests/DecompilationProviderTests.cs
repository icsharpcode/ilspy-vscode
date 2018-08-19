// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.TypeSystem;
using ILSpy.Host.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using OmniSharp.Host.Services;
using Xunit;
using Xunit.Abstractions;

namespace ILSpy.Host.Tests
{
    public class DecompilationProviderTests
    {
        private const string testAssemblyPath = "../../../../TestAssembly/bin/TestAssembly.dll";
        private readonly Mock<IMsilDecompilerEnvironment> _mockEnv;
        public Mock<ILoggerFactory> _mockLoggerFactory;

        public DecompilationProviderTests(ITestOutputHelper output)
        {
            Output = output;

            _mockEnv = new Mock<IMsilDecompilerEnvironment>();
            _mockEnv.Setup(env => env.AssemblyPath).Returns(string.Empty);
            _mockEnv.Setup(env => env.DecompilerSettings).Returns(new ICSharpCode.Decompiler.DecompilerSettings());

            _mockLoggerFactory = new Mock<ILoggerFactory>();
        }

        protected ITestOutputHelper Output { get; }

        [Fact]
        public void AddValidAssemblyShouldSucceed()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            var added = provider.AddAssembly(new FileInfo(testAssemblyPath).FullName);

            // Assert
            Assert.True(added, "Adding a valid managed assembly should return true");
        }

        [Fact(Skip = "Cannot mock ILoggerFactory.CreateLogger() because it is an extension method")]
        public void AddInValidAssemblyShouldFail()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            var added = provider.AddAssembly("../non/existant/path.dll");

            // Assert
            Assert.False(added, "Adding an invalid managed assembly should return false");
        }

        [Fact]
        public void ListOfTypesFromValidAssembly()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var types = provider.ListTypes(assemblyPath, "TestAssembly").ToList();

            // Assert
            Assert.NotEmpty(types);
            Assert.True(types.Single(t => t.Name.Equals("C")).MemberSubKind == TypeKind.Class);
            Assert.True(types.Single(t => t.Name.Equals("S")).MemberSubKind == TypeKind.Struct);
            Assert.True(types.Single(t => t.Name.Equals("I")).MemberSubKind == TypeKind.Interface);
            Assert.True(types.Single(t => t.Name.Equals("E")).MemberSubKind == TypeKind.Enum);
        }

        [Fact]
        public void DecompileAssembly()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var code = provider.GetCode(assemblyPath, EntityHandle.AssemblyDefinition)[LanguageNames.CSharp];

            // Assert
            Assert.Contains("// TestAssembly, Version=", code);
            Assert.Contains("// Architecture: AnyCPU (64-bit preferred)", code);
            Assert.Contains("// Runtime: v4.0.30319", code);
            Assert.Contains("[assembly: AssemblyTitle(\"TestAssembly\")]", code);
            Assert.Contains("[assembly: AssemblyVersion(\"1.0.0.0\")]", code);
            Assert.Contains("[assembly: Guid(\"98030de1-dc87-4e74-8201-fe8e93e826b5\")]", code);
        }

        [Fact]
        public void ListOfMembersOfAType()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var types = provider.ListTypes(assemblyPath, "TestAssembly").ToList();

            // Assert
            var type = types.Single(t => t.Name.Equals("C"));
            var members = provider.GetMembers(assemblyPath, MetadataTokens.TypeDefinitionHandle(type.Token));

            Assert.NotEmpty(members);
            var m1 = members.Single(m => m.Name.Equals("C(int)"));
            Assert.Equal(HandleKind.MethodDefinition, MetadataTokens.EntityHandle(m1.Token).Kind);

            var m2 = members.Single(m => m.Name.Equals("_ProgId"));
            Assert.Equal(HandleKind.FieldDefinition, MetadataTokens.EntityHandle(m2.Token).Kind);

            var m3 = members.Single(m => m.Name.Equals("ProgId"));
            Assert.Equal(HandleKind.PropertyDefinition, MetadataTokens.EntityHandle(m3.Token).Kind);

            var m4 = members.Single(m => m.Name.Equals("NestedC"));
            Assert.Equal(HandleKind.TypeDefinition, MetadataTokens.EntityHandle(m4.Token).Kind);
            Assert.Equal(TypeKind.Class, m4.MemberSubKind);
        }

        [Fact]
        public void DecompileOneMember()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var types = provider.ListTypes(assemblyPath, "TestAssembly").ToList();

            var type = types.Single(t => t.Name.Equals("C"));
            var members = provider.GetMembers(assemblyPath, MetadataTokens.TypeDefinitionHandle(type.Token)).ToArray();

            // Assert
            Assert.NotEmpty(members);
            var m1 = members.Single(m => m.Name.Equals("C(int)"));
            var decompiled = provider.GetCode(assemblyPath, MetadataTokens.EntityHandle(m1.Token))[LanguageNames.CSharp];
            Assert.Equal(@"public C(int ProgramId)
{
	ProgId = ProgramId;
}
", decompiled);
        }

        [Fact]
        public void DecompileNestedClass()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var types = provider.ListTypes(assemblyPath, "TestAssembly").ToList();

            var type = types.Single(t => t.Name.Equals("C"));
            var members = provider.GetMembers(assemblyPath, MetadataTokens.TypeDefinitionHandle(type.Token)).ToArray();

            // Assert
            Assert.NotEmpty(members);
            var m1 = members.Single(m => m.Name.Equals("NestedC"));
            var decompiled = provider.GetCode(assemblyPath, MetadataTokens.EntityHandle(m1.Token))[LanguageNames.CSharp];
            Assert.Equal(
@"public class NestedC
{
	public void M()
	{
	}
}
", decompiled);
        }

        [Fact]
        public void ListNamespace()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;

            // Act
            var added = provider.AddAssembly(assemblyPath);
            var namespaces = provider.ListNamespaces(assemblyPath);

            // Assert
            Assert.Contains("A", namespaces);
            Assert.Contains("A.B", namespaces);
            Assert.Contains("A.B.C", namespaces);
            Assert.Contains("A.B.C.D", namespaces);
            Assert.Contains("TestAssembly", namespaces);
        }

        [Fact]
        public void ListTypesUnderNamespace()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;

            // Act
            var added = provider.AddAssembly(assemblyPath);
            var list1 = provider.ListTypes(assemblyPath, "A");
            var list2 = provider.ListTypes(assemblyPath, "A.B");
            var list3 = provider.ListTypes(assemblyPath, "A.B.C");
            var list4 = provider.ListTypes(assemblyPath, "A.B.C.D");

            // Assert
            Assert.Contains(list1, t => t.Name.Equals("A1"));
            Assert.Contains(list1, t => t.Name.Equals("A2"));
            Assert.Contains(list1, t => t.Name.Equals("A3"));

            Assert.Contains(list2, t => t.Name.Equals("AB1"));
            Assert.Contains(list2, t => t.Name.Equals("AB2"));
            Assert.Contains(list2, t => t.Name.Equals("AB3"));
            Assert.Contains(list2, t => t.Name.Equals("AB4"));

            Assert.Contains(list3, t => t.Name.Equals("ABC1"));
            Assert.Contains(list3, t => t.Name.Equals("ABC2"));
            Assert.Contains(list3, t => t.Name.Equals("ABC3"));
            Assert.Contains(list3, t => t.Name.Equals("ABC4"));

            Assert.Contains(list4, t => t.Name.Equals("ABCD1"));
            Assert.Contains(list4, t => t.Name.Equals("ABCD2"));
        }

        [Fact]
        public void ListGenericTypesWithSameNameButDifferentNumbersOfTypeArguments()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;

            // Act
            var added = provider.AddAssembly(assemblyPath);
            var list1 = provider.ListTypes(assemblyPath, "Generics");

            // Assert
            Assert.Contains(list1, t => t.Name.Equals("C<T>"));
            Assert.Contains(list1, t => t.Name.Equals("C<T1,T2>"));
            Assert.Contains(list1, t => t.Name.Equals("I<T>"));
            Assert.Contains(list1, t => t.Name.Equals("I<T1,T2>"));
            Assert.Contains(list1, t => t.Name.Equals("I<T1,T2,T3>"));
        }

        [Fact]
        public void ListGenericMethodOverloadsWithDifferentNumbersOfTypeArguments()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;

            // Act
            var added = provider.AddAssembly(assemblyPath);
            var list1 = provider.ListTypes(assemblyPath, "Generics");
            var type = list1.Single(t => t.Name.Equals("A"));
            var members = provider.GetMembers(assemblyPath, MetadataTokens.TypeDefinitionHandle(type.Token));

            // Assert
            Assert.Contains(members, m => m.Name.Equals("M<T>() : void"));
            Assert.Contains(members, m => m.Name.Equals("M<T1, T2>() : void"));
        }

        [Fact]
        public void ListNestedGenericsWithDifferentNumbersOfTypeArguments()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;

            // Act
            var added = provider.AddAssembly(assemblyPath);
            var list1 = provider.ListTypes(assemblyPath, "Generics");
            var type = list1.Single(t => t.Name.Equals("A"));
            var members = provider.GetMembers(assemblyPath, MetadataTokens.TypeDefinitionHandle(type.Token));

            // Assert
            Assert.Contains(members, m => m.Name.Equals("NestedC<T>"));
            Assert.Contains(members, m => m.Name.Equals("NestedC<T1,T2>"));
        }

        [Fact]
        public void GetILCodeForType()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;

            // Act
            var added = provider.AddAssembly(assemblyPath);
            var list1 = provider.ListTypes(assemblyPath, "TestAssembly");
            var type = list1.Single(t => t.Name.Equals("C"));
            var il = provider.GetCode(assemblyPath, MetadataTokens.TypeDefinitionHandle(type.Token))[LanguageNames.IL];

            // Assert
            Assert.StartsWith(".class public auto ansi TestAssembly.C", il);
        }

        [Fact]
        public void GetILCodeForMember()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;

            // Act
            var added = provider.AddAssembly(assemblyPath);
            var list1 = provider.ListTypes(assemblyPath, "TestAssembly");
            var type = list1.Single(t => t.Name.Equals("C"));
            var members = provider.GetMembers(assemblyPath, MetadataTokens.TypeDefinitionHandle(type.Token)).ToArray();

            // Assert
            Assert.NotEmpty(members);
            var m1 = members.Single(m => m.Name.Equals("C(int)"));
            var il = provider.GetCode(assemblyPath, MetadataTokens.EntityHandle(m1.Token))[LanguageNames.IL];

            // Assert
            Assert.Equal(@".method /* 06000017 */ public hidebysig specialname rtspecialname 
	instance void .ctor (
		int32 ProgramId
	) cil managed 
{
	// Method begins at RVA 0x2088
	// Code size 17 (0x11)
	.maxstack 8

	IL_0000: ldarg.0
	IL_0001: call instance void [mscorlib]System.Object::.ctor() /* 0A000011 */
	IL_0006: nop
	IL_0007: nop
	IL_0008: ldarg.0
	IL_0009: ldarg.1
	IL_000a: call instance void TestAssembly.C::set_ProgId(int32) /* 06000014 */
	IL_000f: nop
	IL_0010: ret
} // end of method C::.ctor
", il);
        }
    }
}
