using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using ILSpy.Backend.TreeProviders;
using System;
using System.Collections.Generic;

namespace ILSpyX.Backend.TreeProviders.Analyzers;

public class MethodUsedByProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public MethodUsedByProvider(ILSpyXApplication application)
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

