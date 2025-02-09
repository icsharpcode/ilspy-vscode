using System;

namespace ILSpyX.Backend.Model;

[Flags]
public enum NodeFlags
{
    None = 0,
    CompilerGenerated = 1,
    AutoLoaded = 2,
}