using Microsoft.Extensions.Configuration;

namespace MsilDecompiler.WebApi.Configuration
{
    public class ConsoleSourceAssemblyConfiguration: ISourceAssemblyConfiguration
    {
        private readonly string[] _args;

        public string SourceAssemblyFilePath { get; private set; }

        public ConsoleSourceAssemblyConfiguration(ConsoleArgs args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args.Args)
                .Build();

            SourceAssemblyFilePath = config["AssemblyPath"];
        }
    }
}
