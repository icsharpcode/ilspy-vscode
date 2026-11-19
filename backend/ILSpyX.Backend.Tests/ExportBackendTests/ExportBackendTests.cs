using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using Microsoft.Extensions.DependencyInjection;

namespace ILSpyX.Backend.Tests.ExportBackendTests;

public class ExportBackendTests
{
    private sealed class InMemoryExportFileWriter : IExportFileWriter
    {
        public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);

        public Task WriteTextAsync(string path, string contents, CancellationToken cancellationToken)
        {
            Files[path] = contents;
            return Task.CompletedTask;
        }
    }

    private static NodeMetadata CreateAssemblyNodeMetadata() => new()
    {
        AssemblyPath = TestHelper.AssemblyPath,
        Type = NodeType.Assembly,
        Name = "TestAssembly.dll"
    };

    [Fact]
    public async Task ExportAssemblyToSingleCSharpFile()
    {
        var writer = new InMemoryExportFileWriter();
        var services = await TestHelper.CreateTestServicesWithAssembly(writer);
        var exportBackend = services.GetRequiredService<ExportBackend>();

        string outputFile = Path.Combine("export-output", "TestAssembly.cs");
        var result = await exportBackend.ExportNodeToSingleFileAsync(
            CreateAssemblyNodeMetadata(),
            LanguageName.CSharpLatest,
            outputFile,
            includeCompilerGenerated: true,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, result.FilesWritten);
        Assert.Equal(Path.GetFullPath(outputFile), result.OutputTarget);

        Assert.True(writer.Files.TryGetValue(Path.GetFullPath(outputFile), out string? code));
        Assert.Contains("[assembly: AssemblyTitle(\"TestAssembly\")]", code);
        Assert.Contains("namespace TestAssembly", code);
        Assert.Contains("class SomeClass", code);
    }

    [Fact]
    public async Task ExportAssemblyToSingleCSharpFileWithoutCompilerGeneratedTypes()
    {
        var writer = new InMemoryExportFileWriter();
        var services = await TestHelper.CreateTestServicesWithAssembly(writer);
        var exportBackend = services.GetRequiredService<ExportBackend>();

        string outputFile = Path.Combine("export-output", "TestAssembly.cs");
        var result = await exportBackend.ExportNodeToSingleFileAsync(
            CreateAssemblyNodeMetadata(),
            LanguageName.CSharpLatest,
            outputFile,
            includeCompilerGenerated: false,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(writer.Files.TryGetValue(Path.GetFullPath(outputFile), out string? code));
        Assert.Contains("[assembly: AssemblyTitle(\"TestAssembly\")]", code);
        Assert.Contains("namespace TestAssembly", code);
        Assert.Contains("class SomeClass", code);
    }

    [Fact]
    public async Task ExportAssemblyToSingleILFile()
    {
        var writer = new InMemoryExportFileWriter();
        var services = await TestHelper.CreateTestServicesWithAssembly(writer);
        var exportBackend = services.GetRequiredService<ExportBackend>();

        string outputFile = Path.Combine("export-output", "TestAssembly.il");
        var result = await exportBackend.ExportNodeToSingleFileAsync(
            CreateAssemblyNodeMetadata(),
            LanguageName.IL,
            outputFile,
            includeCompilerGenerated: true,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(writer.Files.TryGetValue(Path.GetFullPath(outputFile), out string? code));
        Assert.Contains(".assembly TestAssembly", code);
        Assert.Contains(".class", code);
        Assert.Contains("SomeClass", code);
    }

    [Fact]
    public async Task ExportNonAssemblyNodeToSingleFileFails()
    {
        var services = await TestHelper.CreateTestServicesWithAssembly();
        var exportBackend = services.GetRequiredService<ExportBackend>();

        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.AssemblyPath,
            Type = NodeType.Class,
            Name = "TestAssembly.SomeClass"
        };

        var result = await exportBackend.ExportNodeToSingleFileAsync(
            nodeMetadata,
            LanguageName.CSharpLatest,
            Path.Combine("export-output", "unused.cs"),
            includeCompilerGenerated: true,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
    }
}
