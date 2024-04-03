using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using ILSpy.Backend.TreeProviders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AnalyzerNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public AnalyzerNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return application.TreeNodeProviders.Assembly.CreateNodesAsync();
    }
}

