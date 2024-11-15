using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpy.Backend.TreeProviders;

public interface ITreeNodeProvider
{
    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata) => Task.FromResult(Enumerable.Empty<Node>());
    DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage);
}

public class DummyTreeNodeProvider : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }
}
