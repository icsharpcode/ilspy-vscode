using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AssemblyReferenceNodeProvider(DecompilerBackend decompilerBackend) : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        string code = $"// {nodeMetadata.Name}";
        return Task.FromResult(DecompileResult.WithCode(code));
    }

    public async Task<IEnumerable<Node>> CreateNodesAsync(AssemblyFileIdentifier assemblyFile)
    {
        var decompiler = await decompilerBackend.CreateDecompiler(assemblyFile);
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

        return
            references
                .OrderBy(n => n)
                .Select(reference => new Node
                {
                    Metadata = new NodeMetadata
                    {
                        AssemblyPath = assemblyFile.File,
                        BundledAssemblyName = assemblyFile.BundledAssemblyFile,
                        Type = NodeType.AssemblyReference,
                        Name = reference,
                        IsDecompilable = true
                    },
                    DisplayName = reference,
                    Description = string.Empty,
                    MayHaveChildren = false
                });
    }
}