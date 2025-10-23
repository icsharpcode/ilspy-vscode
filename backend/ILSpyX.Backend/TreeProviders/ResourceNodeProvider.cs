using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class ResourceNodeProvider : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.WithCode($"// {nodeMetadata.Name}");
    }

    public Node CreateNode(AssemblyFileIdentifier assemblyFile, Resource entry, string relativePath = "")
    {
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyFile.File,
                BundleSubPath = assemblyFile.BundleSubPath,
                Name = $"{relativePath}/{entry.Name}",
                Type = NodeType.Resource,
                IsDecompilable = true,
            },
            DisplayName = entry.Name,
            Description = entry.Name,
        };
    }
}