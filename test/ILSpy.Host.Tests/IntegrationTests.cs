using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
        private readonly DecompilationProvider _decompilationProvider;
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

            _decompilationProvider = new DecompilationProvider(_mockEnv.Object, _mockLoggerFactory.Object);

            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddSingleton<IMsilDecompilerEnvironment>(_mockEnv.Object);
                    serviceCollection.AddSingleton<ISharedTextWriter>(_mockWriter.Object);
                    serviceCollection.AddSingleton<IDecompilationProvider>(_decompilationProvider);
                })
                .UseStartup<Startup>());

            _client = _server.CreateClient();

            _filePath = new FileInfo(testAssemblyPath).FullName;
        }

        [Fact]
        public async Task End2End()
        {
            var payload1 = new { AssemblyPath = _filePath };
            var obj = await PostRequest<AddAssemblyResponse>("/addassembly", payload1);

            Assert.True(obj.Added);

            var decompiledCode = await PostRequest<DecompileCode>("/decompileassembly", payload1);

            Assert.Contains("// TestAssembly, Version=", decompiledCode.Decompiled);
            Assert.Contains("[assembly: AssemblyProduct(\"TestAssembly\")]", decompiledCode.Decompiled);
            Assert.Contains("[assembly: AssemblyTitle(\"TestAssembly\")]", decompiledCode.Decompiled);

            var data = await PostRequest<ListTypesResponse>("/listtypes", payload1);

            Assert.NotEmpty(data.Types);
            Assert.True(data.Types.Single(t => t.Name.Equals("C")).MemberSubKind == MemberSubKind.Class);
            Assert.True(data.Types.Single(t => t.Name.Equals("S")).MemberSubKind == MemberSubKind.Structure);
            Assert.True(data.Types.Single(t => t.Name.Equals("I")).MemberSubKind == MemberSubKind.Interface);
            Assert.True(data.Types.Single(t => t.Name.Equals("E")).MemberSubKind == MemberSubKind.Enum);

            var payload2 = new { AssemblyPath = _filePath, Rid = 2 };
            decompiledCode = await PostRequest<DecompileCode>("/decompiletype", payload2);
            Assert.Contains(@"namespace TestAssembly", decompiledCode.Decompiled);
            Assert.Contains(@"public class C", decompiledCode.Decompiled);
            Assert.Contains(@"public C(int ProgramId)", decompiledCode.Decompiled);

            var data2 = await PostRequest<ListMembersResponse>("/listmembers", payload2);

            Assert.NotEmpty(data2.Members);
            Assert.True(data2.Members.Single(t => t.Name.Equals("get_ProgId()")).MemberSubKind == MemberSubKind.None);
            Assert.True(data2.Members.Single(t => t.Name.Equals("set_ProgId(System.Int32)")).MemberSubKind == MemberSubKind.None);

            var payload3 = new { AssemblyPath = _filePath, TypeRid = 2, MemberType = 100663296, MemberRid = 2 };
            decompiledCode = await PostRequest<DecompileCode>("/decompilemember", payload3);

            Assert.Contains("public void set_ProgId(int value)", decompiledCode.Decompiled);
        }

        private async Task<T> PostRequest<T>(string endpoint, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(endpoint, httpContent);
            var responseString = await response.Content.ReadAsStringAsync();
            T result = JsonConvert.DeserializeObject<T>(responseString);
            return result;
        }
    }
}
