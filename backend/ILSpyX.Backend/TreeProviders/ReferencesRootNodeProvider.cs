using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class ReferencesRootNodeProvider(
    AssemblyReferenceNodeProvider assemblyReferenceNodeProvider,
    DecompilerBackend decompilerBackend)
    : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        string code = string.Join('\n',
            GetAssemblyReferences(nodeMetadata.AssemblyPath)
                .Select(reference => $"// {reference}"));
        return DecompileResult.WithCode(code);
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.ReferencesRoot)
        {
            return [];
        }

        return await assemblyReferenceNodeProvider.CreateNodesAsync(nodeMetadata.AssemblyPath);
    }

    private IEnumerable<string> GetAssemblyReferences(string assemblyPath)
    {
        var decompiler = decompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return [];
        }

        HashSet<string> references = new(decompiler.TypeSystem.NameComparer);
        var metadataFile = decompiler.TypeSystem.MainModule.MetadataFile;
        if (metadataFile != null)
        {
            foreach (var ar in metadataFile.AssemblyReferences)
            {
                references.Add(ar.FullName);
            }
        }

        return references.OrderBy(n => n);
    }

    public Node CreateNode(string assemblyPath)
    {
        return new Node(
            new NodeMetadata(
                assemblyPath,
                NodeType.ReferencesRoot,
                "References",
                0,
                0),
            "References",
            string.Empty,
            true
        );
    }
}