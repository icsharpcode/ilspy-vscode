using ICSharpCode.Decompiler;
using Microsoft.Extensions.Logging;

namespace OmniSharp.Host.Services
{
    public interface IMsilDecompilerEnvironment
    {
        string AssemblyPath { get; }

        int Port { get; }
        int HostProcessId { get; }
        LogLevel LogLevel { get; }
        TransportType TransportType { get; }

        string[] AdditionalArguments { get; }

        DecompilerSettings DecompilerSettings { get; }

    }
}