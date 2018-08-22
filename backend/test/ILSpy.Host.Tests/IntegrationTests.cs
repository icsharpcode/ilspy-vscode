// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.TypeSystem;
using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using OmniSharp.Host.Services;
using OmniSharp.Stdio.Services;
using Xunit;

namespace ILSpy.Host.Tests
{
    public class IntegrationTests
    {
        private const string testAssemblyPath = "../../../../TestAssembly/bin/TestAssembly.dll";

        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly Mock<IMsilDecompilerEnvironment> _mockEnv;
        private readonly Mock<ISharedTextWriter> _mockWriter;
        private Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly SimpleDecompilationProvider _decompilationProvider;
        private readonly string _filePath;

        public IntegrationTests()
        {
            _mockEnv = new Mock<IMsilDecompilerEnvironment>();
            _mockEnv.Setup(env => env.AssemblyPath).Returns(string.Empty);
            _mockEnv.Setup(env => env.DecompilerSettings).Returns(new ICSharpCode.Decompiler.DecompilerSettings());

            _mockWriter = new Mock<ISharedTextWriter>();
            _mockWriter.Setup(writer => writer.WriteLine(It.IsAny<object>())).Verifiable();
            _mockWriter.Setup(writer => writer.WriteLineAsync(It.IsAny<object>())).Returns(Task.Delay(1));

            _mockLoggerFactory = new Mock<ILoggerFactory>();

            _decompilationProvider = new SimpleDecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddSingleton<IMsilDecompilerEnvironment>(_mockEnv.Object);
                    serviceCollection.AddSingleton<ISharedTextWriter>(_mockWriter.Object);
                    serviceCollection.AddSingleton<IDecompilationProvider>(_decompilationProvider);
                })
                .UseStartup<Startup>());

            _client = _server.CreateClient();
            if (Debugger.IsAttached)
            {
                _client.Timeout = TimeSpan.FromMinutes(10);
            }

            _filePath = new FileInfo(testAssemblyPath).FullName;
        }

        [Fact]
        public async Task End2End()
        {
            var payloadAddAssembly = new { AssemblyPath = _filePath };
            var obj = await PostRequest<AddAssemblyResponse>("/addassembly", payloadAddAssembly);

            Assert.True(obj.Added);

            var decompiledCode = await PostRequest<DecompileCode>("/decompileassembly", payloadAddAssembly);
            string csharpCode = decompiledCode.Decompiled[LanguageNames.CSharp];
            Assert.Contains("// TestAssembly, Version=", csharpCode);
            Assert.Contains("// Architecture: AnyCPU (64-bit preferred)", csharpCode);
            Assert.Contains("// Runtime: v4.0.30319", csharpCode);

            var payloadListTypes = new { AssemblyPath = _filePath, Namespace = "TestAssembly" };
            var data = await PostRequest<ListTypesResponse>("/listtypes", payloadListTypes);

            Assert.NotEmpty(data.Types);
            Assert.True(data.Types.Single(t => t.Name.Equals("C")).MemberSubKind == TypeKind.Class);
            Assert.True(data.Types.Single(t => t.Name.Equals("S")).MemberSubKind == TypeKind.Struct);
            Assert.True(data.Types.Single(t => t.Name.Equals("I")).MemberSubKind == TypeKind.Interface);
            Assert.True(data.Types.Single(t => t.Name.Equals("E")).MemberSubKind == TypeKind.Enum);

            var c = data.Types.Single(t => t.Name.Equals("C"));
            var payload2 = new { AssemblyPath = _filePath, Handle = c.Token };
            decompiledCode = await PostRequest<DecompileCode>("/decompiletype", payload2);
            csharpCode = decompiledCode.Decompiled[LanguageNames.CSharp];
            Assert.Contains(@"namespace TestAssembly", csharpCode);
            Assert.Contains(@"public class C", csharpCode);
            Assert.Contains(@"public C(int ProgramId)", csharpCode);

            var data2 = await PostRequest<ListMembersResponse>("/listmembers", payload2);

            Assert.NotEmpty(data2.Members);

            var m1 = data2.Members.Single(t => t.Name.Equals("C(int)"));
            Assert.Equal(TypeKind.None, m1.MemberSubKind);
            Assert.Equal(HandleKind.MethodDefinition, MetadataTokens.EntityHandle(m1.Token).Kind);

            var m2 = data2.Members.Single(t => t.Name.Equals("_ProgId"));
            Assert.Equal(TypeKind.None, m2.MemberSubKind);
            Assert.Equal(HandleKind.FieldDefinition, MetadataTokens.EntityHandle(m2.Token).Kind);

            var m3 = data2.Members.Single(t => t.Name.Equals("ProgId"));
            Assert.Equal(TypeKind.None, m3.MemberSubKind);
            Assert.Equal(HandleKind.PropertyDefinition, MetadataTokens.EntityHandle(m3.Token).Kind);

            var payload3 = new { AssemblyPath = _filePath, Type = c.Token, Member = m1.Token };
            decompiledCode = await PostRequest<DecompileCode>("/decompilemember", payload3);

            Assert.Equal(@"public C(int ProgramId)
{
	ProgId = ProgramId;
}
", decompiledCode.Decompiled[LanguageNames.CSharp]);

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
", decompiledCode.Decompiled[LanguageNames.IL]);
        }

        private async Task<T> PostRequest<T>(string endpoint, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(endpoint, httpContent);
            var responseString = await response.Content.ReadAsStringAsync();

            var traceWriter = new Newtonsoft.Json.Serialization.MemoryTraceWriter();
            JsonSerializerSettings settings = new JsonSerializerSettings { TraceWriter = traceWriter, TypeNameHandling = TypeNameHandling.Objects };
            var result = JsonConvert.DeserializeObject<T>(responseString, settings);
            var s = traceWriter.ToString();
            return result;
        }
    }
}
