// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ILSpy.Host.Internal;
using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp;
using OmniSharp.Host.Services;
using OmniSharp.Stdio;
using OmniSharp.Stdio.Services;
using OmniSharp.Utilities;

namespace ILSpy.Host
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
#if NET46
                if (PlatformHelper.IsMono)
                {
                    // Mono uses ThreadPool threads for its async/await implementation.
                    // Ensure we have an acceptable lower limit on the threadpool size to avoid deadlocks and ThreadPool starvation.
                    const int MIN_WORKER_THREADS = 8;

                    System.Threading.ThreadPool.GetMinThreads(out int currentWorkerThreads, out int currentCompletionPortThreads);

                    if (currentWorkerThreads < MIN_WORKER_THREADS)
                    {
                        System.Threading.ThreadPool.SetMinThreads(MIN_WORKER_THREADS, currentCompletionPortThreads);
                    }
                }
#endif

                return Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                return 0xbad;
            }

            //var host = new WebHostBuilder()
            //    .UseKestrel()
            //    .UseContentRoot(Directory.GetCurrentDirectory())
            //    .UseIISIntegration()
            //    .UseStartup<Startup>()
            //    .ConfigureServices(services => services
            //        .AddSingleton(new ConsoleArgs(args))
            //        .AddSingleton<IDecompilationConfiguration, ConsoleSettingsConfiguration>()
            //        .AddSingleton<IDecompilationProvider, DecompilationProvider>())
            //    .Build();

            //host.Run();
        }

        private static int Run(string[] args)
        {
            Console.WriteLine($"ILSpy.Host: {string.Join(" ", args)}");

            var msilDecompilerApp = new CommandLineApplication(throwOnUnexpectedArg: false);
            msilDecompilerApp.HelpOption("-? | -h | --help");

            var assemblyPathOption = msilDecompilerApp.Option("-a | --assembly", "Path to the managed assembly to decompile", CommandOptionType.SingleValue);
            var portOption = msilDecompilerApp.Option("-p | --port", "ILSpy.Host port (defaults to 2000).", CommandOptionType.SingleValue);
            var logLevelOption = msilDecompilerApp.Option("-l | --loglevel", "Level of logging (defaults to 'Information').", CommandOptionType.SingleValue);
            var verboseOption = msilDecompilerApp.Option("-v | --verbose", "Explicitly set 'Debug' log level.", CommandOptionType.NoValue);
            var hostPidOption = msilDecompilerApp.Option("-hpid | --hostPID", "Host process ID.", CommandOptionType.SingleValue);
            var stdioOption = msilDecompilerApp.Option("-stdio | --stdio", "Use STDIO over HTTP as ILSpy.Host communication protocol.", CommandOptionType.NoValue);
            var encodingOption = msilDecompilerApp.Option("-e | --encoding", "Input / output encoding for STDIO protocol.", CommandOptionType.SingleValue);
            var serverInterfaceOption = msilDecompilerApp.Option("-i | --interface", "Server interface address (defaults to 'localhost').", CommandOptionType.SingleValue);

            msilDecompilerApp.OnExecute(() =>
            {
                var assemblyPath = assemblyPathOption.GetValueOrDefault<string>(null);
                var serverPort = portOption.GetValueOrDefault(2000);
                var logLevel = verboseOption.HasValue() ? LogLevel.Debug : logLevelOption.GetValueOrDefault(LogLevel.Information);
                var hostPid = hostPidOption.GetValueOrDefault(-1);
                var transportType = stdioOption.HasValue() ? TransportType.Stdio : TransportType.Http;
                var serverInterface = serverInterfaceOption.GetValueOrDefault("localhost");
                var encodingString = encodingOption.GetValueOrDefault<string>(null);
                var otherArgs = msilDecompilerApp.RemainingArguments.Distinct();

                var env = new MsilDecompilerEnvironment(assemblyPath, serverPort, hostPid, logLevel, transportType, otherArgs.ToArray());

                var config = new ConfigurationBuilder()
                    .AddCommandLine(new[] { "--server.urls", $"http://{serverInterface}:{serverPort}" });

                // If the --encoding switch was specified, we need to set the InputEncoding and OutputEncoding before
                // constructing the SharedConsoleWriter. Otherwise, it might be created with the wrong encoding since
                // it wraps around Console.Out, which gets recreated when OutputEncoding is set.
                if (transportType == TransportType.Stdio && encodingString != null)
                {
                    var encoding = Encoding.GetEncoding(encodingString);
                    Console.InputEncoding = encoding;
                    Console.OutputEncoding = encoding;
                }

                var writer = new SharedConsoleWriter();

                var builder = new WebHostBuilder()
                    .UseConfiguration(config.Build())
                    .UseEnvironment("ILSpyHost")
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddSingleton<IMsilDecompilerEnvironment>(env);
                        serviceCollection.AddSingleton<ISharedTextWriter>(writer);
                        serviceCollection.AddSingleton<IDecompilationProvider, SimpleDecompilationProvider>();
                    })
                    .UseStartup(typeof(Startup));

                if (transportType == TransportType.Stdio)
                {
                    builder.UseServer(new StdioServer(Console.In, writer));
                }
                else
                {
                    builder.UseKestrel();
                }

                using (var app = builder.Build())
                {
                    app.Start();

                    var appLifeTime = app.Services.GetRequiredService<IApplicationLifetime>();

                    Console.CancelKeyPress += (sender, e) =>
                    {
                        appLifeTime.StopApplication();
                        e.Cancel = true;
                    };

                    if (hostPid != -1)
                    {
                        try
                        {
                            var hostProcess = Process.GetProcessById(hostPid);
                            hostProcess.EnableRaisingEvents = true;
                            hostProcess.OnExit(() => appLifeTime.StopApplication());
                        }
                        catch
                        {
                            // If the process dies before we get here then request shutdown
                            // immediately
                            appLifeTime.StopApplication();
                        }
                    }

                    appLifeTime.ApplicationStopping.WaitHandle.WaitOne();
                }

                return 0;
            });

            return msilDecompilerApp.Execute(args.ToArray());
        }
    }
}
