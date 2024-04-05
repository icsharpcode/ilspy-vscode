using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpy.Backend.TreeProviders;

public class AnalyzersRootNodesProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public AnalyzersRootNodesProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

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

