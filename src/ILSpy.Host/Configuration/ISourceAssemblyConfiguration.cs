// See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler;

namespace ILSpy.Host.Configuration
{
    public interface IDecompilationConfiguration
    {
        string FilePath { get; }
        DecompilerSettings DecompilerSettings { get; }
    }
}
