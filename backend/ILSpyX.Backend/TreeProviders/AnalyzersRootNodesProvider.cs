using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AnalyzersRootNodesProvider(AnalyzerNodeProvider analyzerNodeProvider, AnalyzerBackend analyzerBackend)
    : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return Task.FromResult(DecompileResult.Empty());
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return
            (await Task.WhenAll(analyzerBackend.Analyzers.Select(analyzer =>
                analyzerNodeProvider.CreateNode(nodeMetadata, analyzer))))
            .OfType<Node>();
    }
}