using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;

namespace ILSpyX.Backend.Tests;

public class NuGetPackageNodeProviderTests
{
    [Fact]
    public async Task GetRootNodesWithNuGetPackage()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var list = await services.GetRequiredService<AssemblyTreeRootNodesProvider>().GetChildrenAsync(null);
        var node = Assert.Single(list);
        Assert.Equal("TestAssembly.1.0.0", node.DisplayName);
        Assert.Equal(TestHelper.NuGetPackagePath, node.Description);
        Assert.True(node.MayHaveChildren);
        Assert.Equal(TestHelper.NuGetPackagePath, node.Metadata?.AssemblyPath);
        Assert.Equal(Path.GetFileName(TestHelper.NuGetPackagePath), node.Metadata?.Name);
        Assert.Equal(NodeType.NuGetPackage, node.Metadata?.Type);
    }

    [Fact]
    public async Task GetRootPackageFolderItems()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath, Type = NodeType.NuGetPackage, Name = "TestAssembly",
        };
        var list = await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
            .GetChildrenAsync(nodeMetadata);

        Assert.Collection(list,
            node => {
                Assert.Equal("/_rels", node.Metadata?.Name);
                Assert.Equal("_rels", node.DisplayName);
                Assert.Equal(NodeType.PackageFolder, node.Metadata?.Type);
                Assert.True(node.MayHaveChildren);
            },
            node => {
                Assert.Equal("/lib/net8.0", node.Metadata?.Name);
                Assert.Equal("lib/net8.0", node.DisplayName);
                Assert.Equal(NodeType.PackageFolder, node.Metadata?.Type);
                Assert.True(node.MayHaveChildren);
            },
            node => {
                Assert.Equal("/package/services/metadata/core-properties", node.Metadata?.Name);
                Assert.Equal("package/services/metadata/core-properties", node.DisplayName);
                Assert.Equal(NodeType.PackageFolder, node.Metadata?.Type);
                Assert.True(node.MayHaveChildren);
            },
            node => {
                Assert.Equal("/[Content_Types].xml", node.Metadata?.Name);
                Assert.Equal("[Content_Types].xml", node.DisplayName);
                Assert.Equal(NodeType.Resource, node.Metadata?.Type);
                Assert.False(node.MayHaveChildren);
            },
            node => {
                Assert.Equal("/TestAssembly.nuspec", node.Metadata?.Name);
                Assert.Equal("TestAssembly.nuspec", node.DisplayName);
                Assert.Equal(NodeType.Resource, node.Metadata?.Type);
                Assert.False(node.MayHaveChildren);
            }
        );
    }

    [Fact]
    public async Task GetSubFolderWithAssembly()
    {
        var services = await TestHelper.CreateTestServicesWithNuGetPackage();
        var nodeMetadata = new NodeMetadata
        {
            AssemblyPath = TestHelper.NuGetPackagePath, Type = NodeType.PackageFolder, Name = "lib/net8.0",
        };
        var list = await services.GetRequiredService<TreeNodeProviders>().ForNode(nodeMetadata)
            .GetChildrenAsync(nodeMetadata);

        var test = services.GetRequiredService<SingleThreadAssemblyList>();

        var node = Assert.Single(list);
        Assert.Equal("TestAssembly, 1.0.0.0, .NETCoreApp, v8.0", node.DisplayName);
        // Assert.Equal(TestHelper.AssemblyPath, node.Description);
        Assert.Equal("TestAssembly.dll", node.Description);
        Assert.True(node.MayHaveChildren);
        // Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
        Assert.Equal("TestAssembly.dll", node.Metadata?.AssemblyPath);
        Assert.Equal(Path.GetFileName(TestHelper.AssemblyPath), node.Metadata?.Name);
        Assert.Equal(NodeType.Assembly, node.Metadata?.Type);
    }
}