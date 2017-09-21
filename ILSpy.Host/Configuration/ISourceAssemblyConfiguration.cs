using ICSharpCode.Decompiler;

namespace MsilDecompiler.Host.Configuration
{
    public interface IDecompilationConfiguration
    {
        string FilePath { get; }
        DecompilerSettings DecompilerSettings { get; }
    }
}
