using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AssemblyReferenceNodeProvider(ILSpyXApplication application) : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        var code = $"// {nodeMetadata.Name}";
        return DecompileResult.WithCode(code);
    }

    public Task<IEnumerable<Node>> CreateNodesAsync(string assemblyPath)
    {
        var decompiler = application.DecompilerBackend.CreateDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        HashSet<string> references = new(decompiler.TypeSystem.NameComparer);
        foreach (var ar in decompiler.TypeSystem.MainModule.MetadataFile.AssemblyReferences)
        {
            references.Add(ar.FullName);
        }
        return Task.FromResult(
            references
                .OrderBy(n => n)
                .Select(reference => new Node(
                        new NodeMetadata(
                            AssemblyPath: assemblyPath,
                            Type: NodeType.AssemblyReference,
                            Name: reference,
                            SymbolToken: 0,
                            ParentSymbolToken: 0),
                        DisplayName: reference,
                        Description: string.Empty,
                        MayHaveChildren: false,
                        SymbolModifiers: SymbolModifiers.None
                    ))
        );
    }
}

