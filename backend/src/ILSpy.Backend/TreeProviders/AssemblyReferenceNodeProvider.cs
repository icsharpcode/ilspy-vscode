using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ILSpy.Backend.TreeProviders;

public class AssemblyReferenceNodeProvider : ITreeNodeProvider
{
    private readonly ILSpyXApplication application;

    public AssemblyReferenceNodeProvider(ILSpyXApplication application)
    {
        this.application = application;
    }

    public IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata)
    {
        var code = $"// {nodeMetadata.Name}";
        return new Dictionary<string, string>
        {
            [LanguageNames.CSharp] = code,
            [LanguageNames.IL] = code,
        };
    }

    public IEnumerable<Node> CreateNodes(string assemblyPath)
    {
        var decompiler = application.DecompilerBackend.GetDecompiler(assemblyPath);
        if (decompiler is null)
        {
            return Enumerable.Empty<Node>();
        }

        HashSet<string> references = new(decompiler.TypeSystem.NameComparer);
        foreach (var ar in decompiler.TypeSystem.MainModule.PEFile.AssemblyReferences)
        {
            references.Add(ar.FullName);
        }
        return references
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
                )); ;
    }
}

