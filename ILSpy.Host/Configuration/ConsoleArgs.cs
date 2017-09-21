namespace MsilDecompiler.Host.Configuration
{
    public class ConsoleArgs
    {
        public ConsoleArgs(string[] args)
        {
            Args = args;
        }

        public string[] Args { get; }
    }
}
