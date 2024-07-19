using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILSpy.Backend.TreeProviders;

public class RootNodesProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public RootNodesProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return await application.TreeNodeProviders.Assembly.CreateNodesAsync();
    }
}

