using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class ReferencesRootNodeProvider(ILSpyXApplication application) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        var code = string.Join('\n',
            GetAssemblyReferences(nodeMetadata.AssemblyPath)
                .Select(reference => $"// {reference}"));
        return DecompileResult.WithCode(code);
    }

    private IEnumerable<string> GetAssemblyReferences(string assemblyPath)
    {
        var decompiler = application.DecompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return Enumerable.Empty<string>();
        }

        HashSet<string> references = new(decompiler.TypeSystem.NameComparer);
        foreach (var ar in decompiler.TypeSystem.MainModule.MetadataFile.AssemblyReferences)
        {
            references.Add(ar.FullName);
        }
        return references.OrderBy(n => n);
    }

    public Node CreateNode(string assemblyPath)
    {
        return new Node(
            new NodeMetadata(
                AssemblyPath: assemblyPath,
                Type: NodeType.ReferencesRoot,
                Name: "References",
                SymbolToken: 0,
                ParentSymbolToken: 0),
            DisplayName: "References",
            Description: string.Empty,
            MayHaveChildren: true,
            SymbolModifiers: SymbolModifiers.None
        );
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.ReferencesRoot)
        {
            return Enumerable.Empty<Node>();
        }

        return await application.TreeNodeProviders.AssemblyReference.CreateNodesAsync(nodeMetadata.AssemblyPath);
    }
}

