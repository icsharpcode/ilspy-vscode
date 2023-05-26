using ILSpy.Backend.Application;
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

    public IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata)
    {
        return null;
    }

    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata)
    {
        return application.TreeNodeProviders.Assembly.CreateNodes();
    }
}

