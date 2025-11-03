using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata.Ecma335;

namespace ILSpyX.Backend.Tests;

public class NuGetPackageResourceFileDecompilationTests
{
    [Fact]
    public async Task XmlFile()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath, Type = NodeType.Resource, Name = "/[Content_Types].xml",
        };
        Assert.Equal(
            $"// /[Content_Types].xml",
            (await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
                .Decompile(nodeMetadata, LanguageName.CSharpLatest)).DecompiledCode);
    }
}