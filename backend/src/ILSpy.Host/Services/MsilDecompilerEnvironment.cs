// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.IO;
using ICSharpCode.Decompiler;
using Microsoft.Extensions.Logging;

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