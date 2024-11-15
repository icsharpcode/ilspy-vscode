using ICSharpCode.ILSpyX;
using ICSharpCode.ILSpyX.Settings;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.TreeProviders;
using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Application;
using ILSpyX.Backend.Search;
using Microsoft.Extensions.Logging;

namespace ILSpy.Backend.Application;

public class ILSpyXApplication
{
    public ILSpyXApplication(ILoggerFactory loggerFactory)
    {
        BackendSettings = new ILSpyBackendSettings();
        SettingsProvider = new DummySettingsProvider();
        AssemblyListManager = new AssemblyListManager(SettingsProvider);
        AssemblyList = AssemblyListManager.CreateDefaultList(AssemblyListManager.DefaultListName);
        DecompilerBackend = new DecompilerBackend(loggerFactory, BackendSettings, AssemblyList);
        TreeNodeProviders = new TreeNodeProviders(this);
        SearchBackend = new SearchBackend(AssemblyList, BackendSettings);
        AnalyzerBackend = new AnalyzerBackend();
    }

    public ILSpyBackendSettings BackendSettings { get; }

    public DecompilerBackend DecompilerBackend { get; }

    public TreeNodeProviders TreeNodeProviders { get; }

    public ISettingsProvider SettingsProvider { get; }

    public AssemblyList AssemblyList { get; }

    public AssemblyListManager AssemblyListManager { get; }

    public SearchBackend SearchBackend { get; }

    public AnalyzerBackend AnalyzerBackend { get; }
}

