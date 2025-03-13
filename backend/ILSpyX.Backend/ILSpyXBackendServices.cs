using ICSharpCode.ILSpyX;
using ICSharpCode.ILSpyX.Settings;
using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Search;
using ILSpyX.Backend.TreeProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace ILSpyX.Backend;

public class ILSpyXBackendServices : IServiceProvider, ISupportRequiredService
{
    private readonly ServiceCollection serviceCollection = [];
    private readonly ServiceProvider serviceProvider;

    public ILSpyXBackendServices(Action<IServiceCollection>? configureAdditionalServices = null)
    {
        ConfigureServices();
        ConfigureTreeNodeProviders();
        configureAdditionalServices?.Invoke(serviceCollection);

        serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public object? GetService(Type serviceType)
    {
        return serviceProvider.GetService(serviceType);
    }

    public object GetRequiredService(Type serviceType)
    {
        return serviceProvider.GetRequiredService(serviceType);
    }

    private void ConfigureServices()
    {
        serviceCollection
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .AddSingleton<ILSpyBackendSettings>()
            .AddSingleton<ISettingsProvider, DummySettingsProvider>()
            .AddSingleton<AssemblyListManager>()
            .AddSingleton<SingleThreadAssemblyList>()
            .AddSingleton<DecompilerBackend>()
            .AddSingleton<TreeNodeProviders>()
            .AddSingleton<SearchBackend>()
            .AddSingleton<AnalyzerBackend>();
    }

    private void ConfigureTreeNodeProviders()
    {
        serviceCollection.AddSingleton<DummyTreeNodeProvider>()
            .AddSingleton<AssemblyTreeRootNodesProvider>()
            .AddSingleton<AssemblyNodeProvider>()
            .AddSingleton<ReferencesRootNodeProvider>()
            .AddSingleton<AssemblyReferenceNodeProvider>()
            .AddSingleton<NamespaceNodeProvider>()
            .AddSingleton<TypeNodeProvider>()
            .AddSingleton<MemberNodeProvider>()
            .AddSingleton<AnalyzersRootNodesProvider>()
            .AddSingleton<AnalyzerNodeProvider>();
    }
}