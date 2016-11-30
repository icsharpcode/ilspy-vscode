using System;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using MsilDecompiler.MsilSpy;

namespace MsilDecompiler.WebApi.Providers
{
    public class AssemblyNode : BaseNode
    {
        private readonly AssemblyDefinition _assemblyDefinition;

        public AssemblyNode(AssemblyDefinition assemblyDefinition)
        {
            _assemblyDefinition = assemblyDefinition;
        }

        public override void Decompile(Language language, ITextOutput output, DecompilerSettings settings)
        {
            language.DecompileAssembly(_assemblyDefinition, output, settings);
        }
    }
}
