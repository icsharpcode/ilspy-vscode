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
            GetAssemblyReferences(nodeMetadata.GetAssemblyFileIdentifier())
                .Select(reference => $"// {reference}"));
        return DecompileResult.WithCode(code);
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata?.Type != NodeType.ReferencesRoot)
        {
            return [];
        }

        return await assemblyReferenceNodeProvider.CreateNodesAsync(nodeMetadata.GetAssemblyFileIdentifier());
    }

    private IEnumerable<string> GetAssemblyReferences(AssemblyFileIdentifier assemblyFile)
    {
        var decompiler = decompilerBackend.CreateDecompiler(assemblyFile);
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

    public Node CreateNode(AssemblyFileIdentifier assemblyFile)
    {
        return new Node
        {
            Metadata =
                new NodeMetadata
                {
                    AssemblyPath = assemblyFile.File,
                    BundleSubPath = assemblyFile.BundleSubPath,
                    Type = NodeType.ReferencesRoot,
                    Name = "References"
                },
            DisplayName = "References",
            Description = string.Empty,
            MayHaveChildren = true
        };
    }
}