using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpy.Backend.TreeProviders;

public class AssemblyReferenceNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public AssemblyReferenceNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

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

