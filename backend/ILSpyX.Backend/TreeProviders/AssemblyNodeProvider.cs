using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace ILSpy.Backend.TreeProviders;

public class AssemblyNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public AssemblyNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return application.DecompilerBackend.GetCode(
            nodeMetadata.AssemblyPath, EntityHandle.AssemblyDefinition, outputLanguage);
    }

    public async Task<IEnumerable<Node>> CreateNodesAsync()
    {
        return (await application.DecompilerBackend.GetLoadedAssembliesAsync())
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

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.Assembly)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        return Task.FromResult(
            new[] { application.TreeNodeProviders.ReferencesRoot.CreateNode(nodeMetadata.AssemblyPath) }
                .Concat(application.TreeNodeProviders.Namespace.CreateNodes(nodeMetadata.AssemblyPath)));
    }
}

