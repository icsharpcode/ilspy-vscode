using ILSpyX.Backend.LSP.Handlers;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Model;
using Xunit;

namespace ILSpyX.Backend.LSP.Tests;

public class LSPMessageHandlerTests
{
    [Fact]
    public async Task InitWithAssemblies()
    {
        var application = TestHelper.CreateTestApplication();
        var handler = new InitWithAssembliesHandler(application);

        var response = await handler.Handle(new InitWithAssembliesRequest([TestHelper.AssemblyPath]),
            CancellationToken.None);

        Assert.NotNull(response?.LoadedAssemblies);
        Assert.Collection(response?.LoadedAssemblies ?? Enumerable.Empty<AssemblyData>(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });

        Assert.Collection(await application.DecompilerBackend.GetLoadedAssembliesAsync(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });
    }

    [Fact]
    public async Task AddAssembly()
    {
        var application = TestHelper.CreateTestApplication();
        var handler = new AddAssemblyHandler(application);

        var response = await handler.Handle(new AddAssemblyRequest(TestHelper.AssemblyPath),
            CancellationToken.None);

        Assert.True(response.Added);
        Assert.NotNull(response?.AssemblyData);
        Assert.Equal(TestHelper.AssemblyPath, response.AssemblyData.FilePath);

        Assert.Collection(await application.DecompilerBackend.GetLoadedAssembliesAsync(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });
    }

    [Fact]
    public async Task RemoveAssembly()
    {
        var application = TestHelper.CreateTestApplication();

        await application.DecompilerBackend.AddAssemblyAsync(TestHelper.AssemblyPath);
        Assert.Collection(await application.DecompilerBackend.GetLoadedAssembliesAsync(), assemblyData => {
            Assert.Equal(TestHelper.AssemblyPath, assemblyData.FilePath);
        });

        var handler = new RemoveAssemblyHandler(application);

        var response = await handler.Handle(new RemoveAssemblyRequest(TestHelper.AssemblyPath),
            CancellationToken.None);

        Assert.True(response.Removed);
        Assert.Empty(await application.DecompilerBackend.GetLoadedAssembliesAsync());
    }
}