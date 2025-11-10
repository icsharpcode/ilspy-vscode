using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class PackageFolderNodeProvider(ResourceNodeProvider resourceNodeProvider, SingleThreadAssemblyList assemblyList)
    : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return Task.FromResult(DecompileResult.Empty());
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null)
        {
            return [];
        }

        var assembly = assemblyList.FindAssembly(nodeMetadata.AssemblyPath);
        if (assembly is null)
        {
            return [];
        }

        var package = (await assembly.GetLoadResultAsync()).Package;
        if (package is null)
        {
            return [];
        }

        var folder = package.RootFolder;
        string path = "";
        foreach (string pathPart in nodeMetadata.Name.Split('/'))
        {
            var nextFolder = folder.Folders.FirstOrDefault(f => f.Name == pathPart);
            if (nextFolder is null)
            {
                continue;
            }

            folder = nextFolder;
            path += $"{pathPart}/";
        }

        return await GetPackageFolderChildrenAsync(nodeMetadata.AssemblyPath, folder, path);
    }


    public async Task<IEnumerable<Node>> GetPackageFolderChildrenAsync(string packagePath, PackageFolder root,
        string rootPath = "")
    {
        List<Node> children = [];
        foreach (var folder in root.Folders.OrderBy(f => f.Name))
        {
            string newName = folder.Name;
            var subfolder = folder;
            while (subfolder.Folders.Count == 1 && subfolder.Entries.Count == 0)
            {
                // special case: a folder that only contains a single sub-folder
                subfolder = subfolder.Folders[0];
                newName = $"{newName}/{subfolder.Name}";
            }

            children.Add(new Node
            {
                Metadata = new NodeMetadata
                {
                    AssemblyPath = packagePath,
                    BundledAssemblyName = $"{rootPath}/{newName}",
                    Type = NodeType.PackageFolder,
                    Name = $"{rootPath}/{newName}",
                    IsDecompilable = false
                },
                DisplayName = newName,
                Description = newName,
                MayHaveChildren = true,
                SymbolModifiers = SymbolModifiers.None,
            });
        }

        foreach (var entry in root.Entries.OrderBy(e => e.Name))
        {
            if (entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || entry.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var asm = root.ResolveFileName(entry.Name);
                if (asm is not null)
                {
                    var assemblyNode = await AssemblyUtility.CreateAssemblyDataAsync(asm);
                    if (assemblyNode is not null)
                    {
                        children.Add(AssemblyNodeProvider.CreateNode(assemblyNode));
                    }
                }
                else
                {
                    children.Add(
                        resourceNodeProvider.CreateNode(new AssemblyFileIdentifier(packagePath, entry.FullName), entry,
                            rootPath));
                }
            }
            else
            {
                children.Add(resourceNodeProvider.CreateNode(new AssemblyFileIdentifier(packagePath, entry.FullName),
                    entry, rootPath));
            }
        }

        return children;
    }
}