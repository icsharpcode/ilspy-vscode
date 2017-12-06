// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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
using Mono.Cecil;
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

            Assert.Contains("// TestAssembly, Version=", decompiledCode.Decompiled);
            Assert.Contains("// Architecture: AnyCPU (64-bit preferred)", decompiledCode.Decompiled);
            Assert.Contains("// Runtime: .NET 4.0", decompiledCode.Decompiled);

            var payloadListTypes = new { AssemblyPath = _filePath, Namespace = "TestAssembly" };
            var data = await PostRequest<ListTypesResponse>("/listtypes", payloadListTypes);

            Assert.NotEmpty(data.Types);
            Assert.True(data.Types.Single(t => t.Name.Equals("C")).MemberSubKind == MemberSubKind.Class);
            Assert.True(data.Types.Single(t => t.Name.Equals("S")).MemberSubKind == MemberSubKind.Structure);
            Assert.True(data.Types.Single(t => t.Name.Equals("I")).MemberSubKind == MemberSubKind.Interface);
            Assert.True(data.Types.Single(t => t.Name.Equals("E")).MemberSubKind == MemberSubKind.Enum);

            var c = data.Types.Single(t => t.Name.Equals("C"));
            var payload2 = new { AssemblyPath = _filePath, Rid = c.Token.RID };
            decompiledCode = await PostRequest<DecompileCode>("/decompiletype", payload2);
            Assert.Contains(@"namespace TestAssembly", decompiledCode.Decompiled);
            Assert.Contains(@"public class C", decompiledCode.Decompiled);
            Assert.Contains(@"public C(int ProgramId)", decompiledCode.Decompiled);

            var data2 = await PostRequest<ListMembersResponse>("/listmembers", payload2);

            Assert.NotEmpty(data2.Members);

            var m1 = data2.Members.Single(t => t.Name.Equals("C(Int32)"));
            Assert.Equal(MemberSubKind.None, m1.MemberSubKind);
            Assert.Equal(TokenType.Method, m1.Token.TokenType);

            var m2 = data2.Members.Single(t => t.Name.Equals("_ProgId"));
            Assert.Equal(MemberSubKind.None, m2.MemberSubKind);
            Assert.Equal(TokenType.Field, m2.Token.TokenType);

            var m3 = data2.Members.Single(t => t.Name.Equals("ProgId"));
            Assert.Equal(MemberSubKind.None, m3.MemberSubKind);
            Assert.Equal(TokenType.Property, m3.Token.TokenType);

            var payload3 = new { AssemblyPath = _filePath, TypeRid = c.Token.RID, MemberType = 100663296, MemberRid = m1.Token.RID };
            decompiledCode = await PostRequest<DecompileCode>("/decompilemember", payload3);

            Assert.Equal(@"public C(int ProgramId)
{
	this.ProgId = ProgramId;
}
", decompiledCode.Decompiled);
        }

        private async Task<T> PostRequest<T>(string endpoint, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(endpoint, httpContent);
            var responseString = await response.Content.ReadAsStringAsync();

            var traceWriter = new Newtonsoft.Json.Serialization.MemoryTraceWriter();
            JsonSerializerSettings settings = new JsonSerializerSettings { TraceWriter = traceWriter, TypeNameHandling = TypeNameHandling.Objects };
            settings.Converters.Add(new TestMetadataTokenConverter());
            var result = JsonConvert.DeserializeObject<T>(responseString, settings);
            var s = traceWriter.ToString();
            return result;
        }

        // For unknown reason the default converter couldn't deserialize MetadataToken.
        // This works around the issue.
        private class TestMetadataTokenConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(MetadataToken);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var jsonObject = Newtonsoft.Json.Linq.JObject.Load(reader);
                var properties = jsonObject.Properties().ToList();
                var tokenType = (TokenType)(uint)properties[1].Value;
                var rid = (uint)properties[0].Value;
                return new MetadataToken(tokenType, rid);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
