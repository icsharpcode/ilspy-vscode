using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

namespace ILSpy.Backend.TreeProviders;

public class AssemblyNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public AssemblyNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata)
    {
        return application.DecompilerBackend.GetCode(nodeMetadata.AssemblyPath, EntityHandle.AssemblyDefinition);
    }

    public IEnumerable<Node> CreateNodes()
    {
        return application.DecompilerBackend.GetLoadedAssemblies()
            .Select(assemblyData =>
                new Node(
                    new NodeMetadata(
                        AssemblyPath: assemblyData.FilePath,
                        Type: NodeType.Assembly,
                        Name: Path.GetFileName(assemblyData.FilePath),
                        SymbolToken: 0,
                        ParentSymbolToken: 0),
                    DisplayName: GetAssemblyDisplayText(assemblyData),
                    Description: assemblyData.FilePath,
                    MayHaveChildren: true,
                    SymbolModifiers: SymbolModifiers.None
                ));
    }

    private static string GetAssemblyDisplayText(AssemblyData assemblyData)
    {
        return string.Join(", ",
            new[] { assemblyData.Name, assemblyData.Version, assemblyData.TargetFramework }
            .Where(d => d is not null));
    }

    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.Assembly)
        {
            return Enumerable.Empty<Node>();
        }

        return
            new[] { application.TreeNodeProviders.ReferencesRoot.CreateNode(nodeMetadata.AssemblyPath) }
                .Concat(application.TreeNodeProviders.Namespace.CreateNodes(nodeMetadata.AssemblyPath));
    }
}

