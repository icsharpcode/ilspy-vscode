using ILSpy.Backend.Model;

namespace ILSpyX.Backend.Tests;

public class AnalyzerTreeNodeProviderTests
{
    [Fact]
    public async Task MethodAnalyzers()
    {
        var application = await TestHelper.CreateTestApplication();
        var types = await application.TreeNodeProviders.Namespace.GetChildrenAsync(
            new NodeMetadata(TestHelper.AssemblyPath, NodeType.Namespace, "TestAssembly", 0, 0));
        var typeNode = types.First(node => node.Metadata?.Name == "SomeClass");
        var members = await application.TreeNodeProviders.Type.GetChildrenAsync(typeNode.Metadata);
        var methodNode = members.First(node => node.Metadata?.Name?.StartsWith("ToString") ?? false);
        var analyzerNodes = await application.TreeNodeProviders.AnalyzersRoot.GetChildrenAsync(methodNode.Metadata);
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
            await application.TreeNodeProviders.Analyzer.GetChildrenAsync(
                analyzerNodes.First(analyzer => analyzer.Metadata.SubType == "MethodUsedByAnalyzer")?.Metadata);
        var callerStructTypeNode = types.First(node => node.Metadata?.Name == "SomeStruct");
        Assert.Collection(methodUsedByNodes,
            node => {
                Assert.Equal("StructMethod() : string", node.Metadata?.Name);
                Assert.Equal(NodeType.Method, node.Metadata?.Type);
                Assert.Equal(callerStructTypeNode.Metadata?.SymbolToken, node.Metadata?.ParentSymbolToken);
                Assert.Equal(SymbolModifiers.Public, node.SymbolModifiers);
                Assert.False(node.MayHaveChildren);
            });
    }
}