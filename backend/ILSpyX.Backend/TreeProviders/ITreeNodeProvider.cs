using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ILSpy.Backend.TreeProviders;

public interface ITreeNodeProvider
{
    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata) => Enumerable.Empty<Node>();
    DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage);
}
