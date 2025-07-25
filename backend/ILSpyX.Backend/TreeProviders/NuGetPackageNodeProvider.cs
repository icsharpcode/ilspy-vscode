using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class NuGetPackageNodeProvider : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public static Node CreateNode(AssemblyData assemblyData)
    {
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyData.FilePath,
                Type = NodeType.NuGetPackage,
                Name = Path.GetFileName(assemblyData.FilePath),
                IsDecompilable = true
            },
            DisplayName = assemblyData.Name,
            Description = assemblyData.FilePath,
            MayHaveChildren = true,
            SymbolModifiers = SymbolModifiers.None,
            Flags = NodeFlagsHelper.GetNodeFlags(assemblyData)
        };
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        return Task.FromResult(Enumerable.Empty<Node>());
    }
}