using ICSharpCode.Decompiler;
using System;
namespace ILSpy.Backend.Decompiler
{
    public class ILSpySettings
    {
        public ILSpySettings()
        {
        }

        public DecompilerSettings DecompilerSettings { get; } = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            AutomaticProperties = false,
            AutomaticEvents = false
        };
    }
}

