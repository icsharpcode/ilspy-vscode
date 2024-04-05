using ICSharpCode.ILSpyX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILSpy.Backend.Decompiler;

public class SingleThreadAssemblyList
{
    private enum ModificationType
    {
        AddAssembly,
        RemoveAssembly,
    }

    private class ModificationAction
    {
        public ModificationAction(ModificationType type, string assemblyPath, TaskCompletionSource completionSource)
        {
            Type = type;
            AssemblyPath = assemblyPath;
            CompletionSource = completionSource;
        }

        public ModificationType Type { get; }
        public string AssemblyPath { get; }
        public TaskCompletionSource CompletionSource { get; }
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

                if (action is not null && assemblyList is not null)
                {
                    switch (action.Type)
                    {
                        case ModificationType.AddAssembly:
                            assemblyList.Open(action.AssemblyPath);
                            assemblyListManager.SaveList(assemblyList);
                            break;

                        case ModificationType.RemoveAssembly:
                            var assembly = assemblyList.FindAssembly(action.AssemblyPath);
                            if (assembly is not null)
                            {
                                assemblyList.Unload(assembly);
                                assemblyListManager.SaveList(assemblyList);
                            }
                            break;
                    }
                    action.CompletionSource.SetResult();
                }
            }
        });
    }

    public Task AddAssembly(string assemblyPath)
    {
        var completionSource = new TaskCompletionSource();
        modificationActions.Add(new ModificationAction(ModificationType.AddAssembly, assemblyPath, completionSource));
        return completionSource.Task;
    }

    public Task RemoveAssembly(string assemblyPath)
    {
        var completionSource = new TaskCompletionSource();
        modificationActions.Add(new ModificationAction(ModificationType.RemoveAssembly, assemblyPath, completionSource));
        return completionSource.Task;
    }

    public AssemblyList? AssemblyList => assemblyList;

    public async Task<IList<LoadedAssembly>> GetAllAssemblies()
    {
        return assemblyList is not null
            ? await assemblyList.GetAllAssemblies()
            : new List<LoadedAssembly>();
    }
}

