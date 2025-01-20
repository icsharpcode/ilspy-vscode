using ICSharpCode.ILSpyX;
using ICSharpCode.ILSpyX.Settings;
using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Search;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.Logging;

namespace ILSpyX.Backend.Application;

public class ILSpyXApplication
{
    public ILSpyXApplication(ILoggerFactory loggerFactory)
    {
        BackendSettings = new ILSpyBackendSettings();
        SettingsProvider = new DummySettingsProvider();
        AssemblyListManager = new AssemblyListManager(SettingsProvider);
        SingleThreadAssemblyList = new SingleThreadAssemblyList(AssemblyListManager);
        DecompilerBackend = new DecompilerBackend(loggerFactory, BackendSettings, SingleThreadAssemblyList);
        TreeNodeProviders = new TreeNodeProviders(this);
        SearchBackend = new SearchBackend(SingleThreadAssemblyList, BackendSettings);
        AnalyzerBackend = new AnalyzerBackend();
    }

    public ILSpyBackendSettings BackendSettings { get; }

    public DecompilerBackend DecompilerBackend { get; }

    public TreeNodeProviders TreeNodeProviders { get; }

    public ISettingsProvider SettingsProvider { get; }

    public SingleThreadAssemblyList SingleThreadAssemblyList { get; }

    public AssemblyListManager AssemblyListManager { get; }

    public SearchBackend SearchBackend { get; }

    public AnalyzerBackend AnalyzerBackend { get; }
}

