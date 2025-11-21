using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class ResourceNodeProvider(SingleThreadAssemblyList assemblyList) : ITreeNodeProvider
{
    public async Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        if (nodeMetadata.Type != NodeType.Resource)
        {
            return DecompileResult.Empty();
        }

        Resource? resource = null;
        var assembly = assemblyList.FindAssembly(nodeMetadata.AssemblyPath);
        var loadResult = assembly is not null ? await assembly.GetLoadResultAsync() : null;
        if (loadResult?.Package is { } package)
        {
            resource = ResolveResourceRecursively(nodeMetadata, package.RootFolder);
        }

        if (resource is null)
        {
            return DecompileResult.Empty();
        }

        long? sizeInBytes = resource.TryGetLength();
        string sizeInBytesText = sizeInBytes == null ? "" : ", " + sizeInBytes + " bytes";
        string test = $"// {resource.Name} ({resource.ResourceType}, {resource.Attributes}{sizeInBytesText})";
        return DecompileResult.WithCode(
            $"// {resource.Name} ({resource.ResourceType}, {resource.Attributes}{sizeInBytesText})");
    }


    private static Resource? ResolveResourceRecursively(NodeMetadata nodeMetadata, PackageFolder rootFolder)
    {
        var folder = rootFolder;
        string[] pathParts = nodeMetadata.Name?.Split('/') ?? [];
        foreach (string pathPart in pathParts.SkipLast(1))
        {
            if (pathPart == "")
            {
                continue;
            }

            var nextFolder = folder.Folders.FirstOrDefault(f => f.Name == pathPart);
            if (nextFolder is null)
            {
                return null;
            }

            folder = nextFolder;
        }


        if (folder.Entries.FirstOrDefault(entry => entry.Name == pathParts.LastOrDefault()) is
            { } resourceEntry)
        {
            return resourceEntry;
        }

        return null;
    }

    public Node CreateNode(AssemblyFileIdentifier assemblyFile, Resource entry, string relativePath = "")
    {
        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyFile.File,
                BundledAssemblyName = assemblyFile.BundledAssemblyFile,
                Name = $"{relativePath}{entry.Name}",
                Type = NodeType.Resource,
                IsDecompilable = true,
            },
            DisplayName = entry.Name,
            Description = entry.Name,
        };
    }
}