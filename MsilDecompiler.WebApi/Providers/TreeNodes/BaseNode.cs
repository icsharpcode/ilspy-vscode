using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using MsilDecompiler.MsilSpy;

namespace MsilDecompiler.WebApi.Providers
{
    public abstract class BaseNode
    {
        public IMetadataTokenProvider MetadataToken { get; private set; }

        public abstract void Decompile(Language language, ITextOutput output, DecompilerSettings settings);

        public virtual string Text { get { return null; } }

        public virtual string Tooltip { get { return null; } }

        public override string ToString()
        {
            return this.Text ?? string.Empty;
        }

        private List<BaseNode> _children;
        public List<BaseNode> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new List<BaseNode>();
                }

                return _children;
            }
        }
    }
}
