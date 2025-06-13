using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;

namespace ILSpyX.Backend.Tests.AnalyzerTreeNodeProviders;

public class MethodAnalyzersTests
{
    [Fact]
    public async Task UsedBy()
    {
        var application = await TestHelper.CreateTestServicesWithAssembly();
        var treeNodeProviders = application.GetRequiredService<TreeNodeProviders>();
        var types = await treeNodeProviders.Namespace.GetChildrenAsync(
            new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "TestAssembly", 0, 0));
        var typeNode = types.First(node => node.Metadata?.Name == "SomeClass");
        var members = await treeNodeProviders.Type.GetChildrenAsync(typeNode.Metadata);
        var methodNode = members.First(node => node.Metadata?.Name?.StartsWith("ToString") ?? false);
        var analyzerNodes = await treeNodeProviders.AnalyzersRoot.GetChildrenAsync(methodNode.Metadata);
        Assert.Collection(analyzerNodes,
            node => { },
            node => {
                Assert.Equal("Used By", node.DisplayName);
                Assert.Equal("Used By", node.Description);
                Assert.True(node.MayHaveChildren);
                Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
                Assert.Equal(NodeType.Analyzer, node.Metadata?.Type);
                Assert.Equal("MethodUsedByAnalyzer", node.Metadata?.SubType);
                Assert.Equal(methodNode.Metadata?.SymbolToken, node.Metadata?.SymbolToken);
                Assert.Equal(methodNode.Metadata?.ParentSymbolToken, node.Metadata?.ParentSymbolToken);
            },
            node => { },
            node => { });

        var methodUsedByNodes =
            await treeNodeProviders.Analyzer.GetChildrenAsync(
                analyzerNodes.First(analyzer => analyzer.Metadata?.SubType == "MethodUsedByAnalyzer")?.Metadata);
        var callerStructTypeNode = types.First(node => node.Metadata?.Name == "SomeStruct");
        var node = Assert.Single(methodUsedByNodes);
        Assert.Equal("StructMethod() : string", node.DisplayName);
        Assert.Equal("TestAssembly.SomeStruct", node.Description);
        Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
        Assert.Equal(NodeType.Method, node.Metadata?.Type);
        Assert.Equal(callerStructTypeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
        Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
        Assert.False(node.MayHaveChildren);
    }

    [Fact]
    public async Task UsesDotNetFrameworkSymbol()
    {
        var application = await TestHelper.CreateTestServicesWithAssembly();
        var treeNodeProviders = application.GetRequiredService<TreeNodeProviders>();
        var types = await treeNodeProviders.Namespace.GetChildrenAsync(
            new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "TestAssembly", 0, 0));
        var typeNode = types.First(node => node.Metadata?.Name == "SomeClass");
        var members = await treeNodeProviders.Type.GetChildrenAsync(typeNode.Metadata);
        var methodNode = members.First(node => node.Metadata?.Name?.StartsWith("CallsFrameworkMethod") ?? false);
        var analyzerNodes = await treeNodeProviders.AnalyzersRoot.GetChildrenAsync(methodNode.Metadata);
        Assert.Collection(analyzerNodes,
            node => {
                Assert.Equal("Uses", node.DisplayName);
                Assert.Equal("Uses", node.Description);
                Assert.Equal(TestHelper.AssemblyPath, node.Metadata?.AssemblyPath);
                Assert.Equal(NodeType.Analyzer, node.Metadata?.Type);
                Assert.Equal("MethodUsesAnalyzer", node.Metadata?.SubType);
                Assert.Equal(methodNode.Metadata?.SymbolToken, node.Metadata?.SymbolToken);
                Assert.Equal(methodNode.Metadata?.ParentSymbolToken, node.Metadata?.ParentSymbolToken);
                Assert.True(node.MayHaveChildren);
            },
            node => { },
            node => { });

        var methodUsesNodes =
            await treeNodeProviders.Analyzer.GetChildrenAsync(
                analyzerNodes.First(analyzer => analyzer.Metadata?.SubType == "MethodUsesAnalyzer")?.Metadata);
        var node = Assert.Single(methodUsesNodes);
        Assert.Equal("Join(string?, string?[]) : string", node.DisplayName);
        Assert.Equal("System.String", node.Description);
        Assert.Contains("System.Private.CoreLib.dll", node.Metadata?.AssemblyPath);
        Assert.Equal(NodeType.Method, node.Metadata?.Type);
        Assert.Equal(SymbolModifiers.Public | SymbolModifiers.Static, node.SymbolModifiers);
        Assert.False(node.MayHaveChildren);
    }
}