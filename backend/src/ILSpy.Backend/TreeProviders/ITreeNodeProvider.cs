using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ILSpy.Backend.TreeProviders;

public interface ITreeNodeProvider
{
    public IEnumerable<Node> GetChildren(NodeMetadata? nodeMetadata) => Enumerable.Empty<Node>();
    IDictionary<string, string>? Decompile(NodeMetadata nodeMetadata);
}
