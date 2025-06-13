using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AnalyzersRootNodesProvider(TreeNodeProviders treeNodeProviders, AnalyzerBackend analyzerBackend)
    : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return Task.FromResult(
            analyzerBackend.Analyzers.Select(
                    analyzer => treeNodeProviders.Analyzer.CreateNode(nodeMetadata, analyzer))
                .OfType<Node>());
    }
}