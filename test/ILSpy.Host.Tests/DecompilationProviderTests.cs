// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Moq;
using ILSpy.Host.Providers;
using OmniSharp.Host.Services;
using System.IO;
using System.Linq;
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
            var types = provider.ListTypes(assemblyPath).ToList();

            // Assert
            Assert.NotEmpty(types);
            Assert.True(types.Single(t => t.Name.Equals("C")).MemberSubKind == MemberSubKind.Class);
            Assert.True(types.Single(t => t.Name.Equals("S")).MemberSubKind == MemberSubKind.Structure);
            Assert.True(types.Single(t => t.Name.Equals("I")).MemberSubKind == MemberSubKind.Interface);
            Assert.True(types.Single(t => t.Name.Equals("E")).MemberSubKind == MemberSubKind.Enum);
        }

        [Fact]
        public void DecompileAssembly()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var code = provider.GetCode(assemblyPath, TokenType.Assembly, 0);

            // Assert
            Assert.Contains("// TestAssembly, Version=", code);
            Assert.Contains("// Architecture: AnyCPU (64-bit preferred)", code);
            Assert.Contains("// Runtime: .NET 4.0", code);
        }

        [Fact]
        public void ListOfMembersOfAType()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var types = provider.ListTypes(assemblyPath).ToList();

            // Assert
            var type = types.Single(t => t.Name.Equals("C"));
            var members = provider.GetChildren(assemblyPath, type.Token.TokenType, type.Token.RID).ToArray();

            Assert.NotEmpty(members);
            Assert.Equal(".ctor", members[0].Name);
            Assert.Equal(TokenType.Method, members[0].Token.TokenType);

            Assert.Equal("_ProgId", members[1].Name);
            Assert.Equal(TokenType.Field, members[1].Token.TokenType);

            Assert.Equal("ProgId", members[2].Name);
            Assert.Equal(TokenType.Property, members[2].Token.TokenType);
        }

        [Fact]
        public void DecompileOneMember()
        {
            // Arrange
            var provider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            // Act
            string assemblyPath = new FileInfo(testAssemblyPath).FullName;
            var added = provider.AddAssembly(assemblyPath);
            var types = provider.ListTypes(assemblyPath).ToList();

            // Assert
            var type = types.Single(t => t.Name.Equals("C"));
            var members = provider.GetChildren(assemblyPath, type.Token.TokenType, type.Token.RID).ToArray();

            Assert.NotEmpty(members);
            var decompiled = provider.GetMemberCode(assemblyPath, members[0].Token);
            Assert.Equal(@"public C(int ProgramId)
{
	this.ProgId = ProgramId;
}
", decompiled);
        }
    }
}
