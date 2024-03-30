using ILSpy.Backend.Decompiler;
using ILSpy.Backend.TreeProviders;
using ILSpyX.Backend.Application;
using Microsoft.Extensions.Logging;

namespace ILSpy.Backend.Application;

public class ILSpyXApplication
{
    public ILSpyXApplication(ILoggerFactory loggerFactory, ILSpyBackendSettings ilspyBackendSettings)
    {
        DecompilerBackend = new(loggerFactory, ilspyBackendSettings);
        TreeNodeProviders = new(this);
    }

    public DecompilerBackend DecompilerBackend { get; }
    public TreeNodeProviders TreeNodeProviders { get; }
}

