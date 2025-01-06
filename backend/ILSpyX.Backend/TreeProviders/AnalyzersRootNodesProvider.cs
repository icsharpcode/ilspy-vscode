using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AnalyzersRootNodesProvider(ILSpyXApplication application) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return Task.FromResult(
            application.AnalyzerBackend.Analyzers.Select(
                analyzer => application.TreeNodeProviders.Analyzer.CreateNode(nodeMetadata, analyzer))
            .OfType<Node>());
    }
}

