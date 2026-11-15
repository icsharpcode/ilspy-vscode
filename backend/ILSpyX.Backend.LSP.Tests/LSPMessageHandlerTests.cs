using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Handlers;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.Search;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using Xunit;

namespace ILSpyX.Backend.LSP.Tests;

public class LSPMessageHandlerTests
{
    [Fact]
    public async Task InitWithAssemblies()
    {
        var services = TestHelper.CreateTestServices();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var handler = new InitWithAssembliesHandler(decompilerBackend);

        var response = await handler.Handle(new InitWithAssembliesRequest([TestHelper.AssemblyPath]),
            CancellationToken.None);

        Assert.NotNull(response?.LoadedAssemblies);
        Assert.Collection(response?.LoadedAssemblies ?? Enumerable.Empty<AssemblyData>(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });

        Assert.Collection(await decompilerBackend.GetLoadedAssembliesAsync(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });
    }

    [Fact]
    public async Task AddAssembly()
    {
        var services = TestHelper.CreateTestServices();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();

        var handler = new AddAssemblyHandler(decompilerBackend);

        var response = await handler.Handle(new AddAssemblyRequest(TestHelper.AssemblyPath),
            CancellationToken.None);

        Assert.True(response.Added);
        Assert.NotNull(response?.AssemblyData);
        Assert.Equal(TestHelper.AssemblyPath, response.AssemblyData.FilePath);

        Assert.Collection(await decompilerBackend.GetLoadedAssembliesAsync(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });
    }

    [Fact]
    public async Task RemoveAssembly()
    {
        var services = TestHelper.CreateTestServices();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();


        await decompilerBackend.AddAssemblyAsync(TestHelper.AssemblyPath);
        Assert.Collection(await decompilerBackend.GetLoadedAssembliesAsync(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });

        var handler = new RemoveAssemblyHandler(decompilerBackend);

        var response = await handler.Handle(new RemoveAssemblyRequest(TestHelper.AssemblyPath),
            CancellationToken.None);

        Assert.True(response.Removed);
        Assert.Empty(await decompilerBackend.GetLoadedAssembliesAsync());
    }

    [Fact]
    public async Task SearchSendsMatchLocation()
    {
        var services = TestHelper.CreateTestServices();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        await decompilerBackend.AddAssemblyAsync(TestHelper.AssemblyPath);
        var handler = new SearchHandler(decompilerBackend, services.GetRequiredService<SearchBackend>());

        var response = await handler.Handle(new SearchRequest("c:Test2", LanguageName.CSharpLatest),
            CancellationToken.None);

        // Serialize like the LSP server does, so the extension really receives the match.
        var json = JObject.Parse(new LspSerializer().SerializeObject(response));
        var result = Assert.Single(json["results"]!);
        Assert.Equal("SomeClass.CallsFrameworkMethod() : string", (string?) result["displayName"]);
        Assert.Equal("Test2", (string?) result["match"]?["text"]);
        Assert.NotNull((int?) result["match"]?["line"]);
        Assert.NotNull((int?) result["match"]?["column"]);
        Assert.Equal(5, (int?) result["match"]?["length"]);
    }
}
