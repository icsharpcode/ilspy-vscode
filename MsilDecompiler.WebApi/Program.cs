using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MsilDecompiler.WebApi.Configuration;
using MsilDecompiler.WebApi.Providers;

namespace MsilDecompiler.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .ConfigureServices(services => services
                    .AddSingleton(new ConsoleArgs(args))
                    .AddSingleton<IDecompilationConfiguration, ConsoleSettingsConfiguration>()
                    .AddSingleton<IDecompilationProvider, DecompilationProvider>())
                .Build();

            host.Run();
        }
    }
}
