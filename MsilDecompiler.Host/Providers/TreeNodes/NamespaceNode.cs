using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using MsilDecompiler.MsilSpy;

namespace MsilDecompiler.Host.Providers
{
    public class NamespaceNode : BaseNode
    {
        private readonly string _name;

        public string Name { get { return _name; } }

        public NamespaceNode(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            this._name = name;
        }

        public override string Text
        {
            get
            {
                return _name.Length == 0 ? "-" : _name;
            }
        }

        public override void Decompile(Language language, ITextOutput output, DecompilerSettings settings)
        {
            language.DecompileNamespace(_name, this.Children.OfType<TypeNode>().Select(t => t.TypeDefinition), output, settings);
        }
    }
}
