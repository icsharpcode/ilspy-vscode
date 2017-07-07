using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using MsilDecompiler.MsilSpy;

namespace MsilDecompiler.Host.Providers
{
    public class TypeNode : BaseNode
    {
        private TypeDefinition _typeDefinition;

        public TypeDefinition TypeDefinition
        {
            get
            {
                return _typeDefinition;
            }
        }

        public override void Decompile(Language language, ITextOutput output, DecompilerSettings settings)
        {
            language.DecompileType(_typeDefinition, output, settings);
        }
    }
}