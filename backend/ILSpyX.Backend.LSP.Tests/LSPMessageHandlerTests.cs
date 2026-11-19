using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Handlers;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Model;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task ExportNodeAsProject()
    {
        var services = TestHelper.CreateTestServices();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var exportBackend = services.GetRequiredService<ExportBackend>();
        var handler = new ExportNodeHandler(exportBackend, decompilerBackend);

        await decompilerBackend.AddAssemblyAsync(TestHelper.AssemblyPath);

        string outputDirectory = Path.Combine(
            Path.GetTempPath(), "ilspy-lsp-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var response = await handler.Handle(
                new ExportNodeRequest(
                    new NodeMetadata
                    {
                        AssemblyPath = TestHelper.AssemblyPath,
                        Type = NodeType.Assembly,
                        Name = "TestAssembly.dll"
                    },
                    LanguageName.CSharpLatest,
                    outputDirectory,
                    IncludeCompilerGenerated: true,
                    ExportOutputType.Project),
                CancellationToken.None);

            Assert.True(response.Succeeded);
            Assert.True(response.FilesWritten > 0);
            Assert.Equal(0, response.ErrorCount);
            Assert.NotNull(response.OutputTarget);
            Assert.True(Directory.Exists(response.OutputTarget));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportNodeAsSingleFile()
    {
        var services = TestHelper.CreateTestServices();
        var decompilerBackend = services.GetRequiredService<DecompilerBackend>();
        var exportBackend = services.GetRequiredService<ExportBackend>();
        var handler = new ExportNodeHandler(exportBackend, decompilerBackend);

        await decompilerBackend.AddAssemblyAsync(TestHelper.AssemblyPath);

        // In SingleFile mode, the output target parameter carries the target file path.
        string outputFile = Path.Combine(
            Path.GetTempPath(), "ilspy-lsp-tests", Guid.NewGuid().ToString("N"), "TestAssembly.cs");
        try
        {
            var response = await handler.Handle(
                new ExportNodeRequest(
                    new NodeMetadata
                    {
                        AssemblyPath = TestHelper.AssemblyPath,
                        Type = NodeType.Assembly,
                        Name = "TestAssembly.dll"
                    },
                    LanguageName.CSharpLatest,
                    outputFile,
                    IncludeCompilerGenerated: true,
                    ExportOutputType.SingleFile),
                CancellationToken.None);

            Assert.True(response.Succeeded);
            Assert.Equal(Path.GetFullPath(outputFile), response.OutputTarget);
            Assert.Equal(1, response.FilesWritten);
            Assert.Equal(0, response.ErrorCount);
            Assert.True(File.Exists(outputFile));
            Assert.Contains("class SomeClass", await File.ReadAllTextAsync(outputFile));
        }
        finally
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
        }
    }
}