using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Decompiler;

public class SingleThreadAssemblyList
{
    private enum ModificationType
    {
        AddAssembly,
        RemoveAssembly,
    }

    private class ModificationAction(
        ModificationType type,
        string assemblyPath,
        TaskCompletionSource<LoadedAssembly?> completionSource)
    {
        public ModificationType Type { get; } = type;
        public string AssemblyPath { get; } = assemblyPath;
        public TaskCompletionSource<LoadedAssembly?> CompletionSource { get; } = completionSource;
    }

    private readonly BlockingCollection<ModificationAction> modificationActions = new();
    private AssemblyList? assemblyList;
    private readonly AssemblyListManager assemblyListManager;

    public SingleThreadAssemblyList(AssemblyListManager assemblyListManager)
    {
        this.assemblyListManager = assemblyListManager;
        Start();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD110:Observe result of async calls")]
    private void Start()
    {
        Task.Run(() => {
            assemblyList = assemblyListManager.CreateDefaultList(AssemblyListManager.DefaultListName);

            while (!modificationActions.IsAddingCompleted)
            {
                ModificationAction? action = null;
                try
                {
                    action = modificationActions.Take();
                }
                catch (InvalidOperationException) { }

                if (action is null || assemblyList is null)
                {
                    continue;
                }

                LoadedAssembly? result = null;

                switch (action.Type)
                {
                    case ModificationType.AddAssembly:
                        var loadedAssembly = assemblyList.Open(action.AssemblyPath);
                        assemblyListManager.SaveList(assemblyList);
                        result = loadedAssembly;
                        break;

                    case ModificationType.RemoveAssembly:
                        var assembly = assemblyList.FindAssembly(action.AssemblyPath);
                        if (assembly is not null)
                        {
                            assemblyList.Unload(assembly);
                            assemblyListManager.SaveList(assemblyList);
                            result = assembly;
                        }

                        break;
                }

                action.CompletionSource.SetResult(result);
            }
        });
    }

    public Task<LoadedAssembly?> AddAssembly(string assemblyPath)
    {
        var completionSource = new TaskCompletionSource<LoadedAssembly?>();
        modificationActions.Add(new ModificationAction(ModificationType.AddAssembly, assemblyPath, completionSource));
        return completionSource.Task;
    }

    public Task<LoadedAssembly?> RemoveAssembly(string assemblyPath)
    {
        var completionSource = new TaskCompletionSource<LoadedAssembly?>();
        modificationActions.Add(new ModificationAction(ModificationType.RemoveAssembly, assemblyPath, completionSource));
        return completionSource.Task;
    }

    public AssemblyList? AssemblyList => assemblyList;

    public IList<LoadedAssembly> GetLoadedAssemblies()
    {
        return assemblyList is not null
            ? assemblyList.GetAssemblies()
            : new List<LoadedAssembly>();
    }

    public async Task<IList<LoadedAssembly>> GetMetadataFileAssemblies()
    {
        return assemblyList is not null
            ? await assemblyList.GetAllAssemblies()
            : new List<LoadedAssembly>();
    }

    public LoadedAssembly? FindAssembly(string file)
    {
        return assemblyList?.FindAssembly(file);
    }

    public async Task<LoadedAssembly?> FindAssembly(AssemblyFileIdentifier assemblyFile)
    {
        if (assemblyFile.BundleSubPath is not null)
        {
            var assembly = FindAssembly(assemblyFile.File);
            var loadResult = assembly is not null ? await assembly.GetLoadResultAsync() : null;
            var package = loadResult?.Package;
            if (package is not null)
            {
                var folder = package.RootFolder;
                string[] pathParts = Path.GetDirectoryName(assemblyFile.BundleSubPath)?.Split('/') ?? [];
                foreach (string folderName in pathParts)
                {
                    var nextFolder = folder.Folders.FirstOrDefault(f => f.Name == folderName);
                    if (nextFolder is null)
                    {
                        break;
                    }

                    folder = nextFolder;
                }

                return folder.ResolveFileName(Path.GetFileName(assemblyFile.BundleSubPath));
            }
        }
        else
        {
            return FindAssembly(assemblyFile.File);
        }

        return null;
    }
}

