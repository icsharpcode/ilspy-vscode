// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using OmniSharp;
using OmniSharp.Stdio.Services;
using OmniSharp.Stdio.Logging;
using OmniSharp.Host.Services;
using ILSpy.Host.MiddleWare;

namespace ILSpy.Host
{
    public class Startup
    {
        private readonly IMsilDecompilerEnvironment _env;

        public Startup(IMsilDecompilerEnvironment env)
        {
            _env = env;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("MSILDECOMPILER_");

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            //TODO: add options, e.g., formatting options for decompiled code, etc.
            // services.Configure<MsilDecompilerOption>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            ISharedTextWriter writer)
        {
            if (_env.TransportType == TransportType.Stdio)
            {
                loggerFactory.AddStdio(writer, (category, level) => LogFilter(category, level, _env));
            }
            else
            {
                loggerFactory.AddConsole((category, level) => LogFilter(category, level, _env));
            }

            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddSerilog();

            app.UseExceptionHandler("/error");

            // Loading all endpoint middleware that implements BaseMiddleWare.
            app.UseEndpointMiddleware();

            app.UseMiddleware<StopServerMiddleware>();

            var logger = loggerFactory.CreateLogger<Startup>();
            if (_env.TransportType == TransportType.Stdio)
            {
                logger.LogInformation($"MsilDecompiler server running using {nameof(TransportType.Stdio)}.");
            }
            else
            {
                logger.LogInformation($"MsilDecompiler server running on port '{_env.Port}'.");
            }
        }
        
        private static bool LogFilter(string category, LogLevel level, IMsilDecompilerEnvironment environment)
        {
            if (environment.LogLevel > level)
            {
                return false;
            }

            if (!category.StartsWith("MsilDecompiler", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
