using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;

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

    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata)
    {
        return application.TreeNodeProviders.Assembly.CreateNodes();
    }
}

