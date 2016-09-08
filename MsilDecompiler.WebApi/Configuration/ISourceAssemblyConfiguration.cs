using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;

namespace MsilDecompiler.WebApi.Configuration
{
    public interface IDecompilationConfiguration
    {
        string FilePath { get; }
        DecompilerSettings DecompilerSettings { get; }
    }
}
