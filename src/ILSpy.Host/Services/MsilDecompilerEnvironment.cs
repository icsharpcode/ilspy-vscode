using ICSharpCode.Decompiler;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace OmniSharp.Host.Services
{
    class MsilDecompilerEnvironment : IMsilDecompilerEnvironment
    {
        public LogLevel LogLevel { get; }

        public int Port { get; }

        public int HostProcessId { get; }

        public string AssemblyPath { get; }

        public TransportType TransportType { get; }

        public string[] AdditionalArguments { get; }

        public DecompilerSettings DecompilerSettings { get; } = new DecompilerSettings();

        public MsilDecompilerEnvironment(
            string path = null,
            int port = -1,
            int hostPid = -1,
            LogLevel traceType = LogLevel.None,
            TransportType transportType = TransportType.Stdio,
            string[] additionalArguments = null)
        {
            if (File.Exists(path))
            {
                AssemblyPath = path;
            }

            Port = port;
            HostProcessId = hostPid;
            LogLevel = traceType;
            TransportType = transportType;
            AdditionalArguments = additionalArguments;
        }

        public static bool IsValidPath(string path)
        {
            return string.IsNullOrEmpty(path)
                || Directory.Exists(path)
                || (File.Exists(path) && Path.GetExtension(path).Equals(".sln", StringComparison.OrdinalIgnoreCase));
        }
    }
}