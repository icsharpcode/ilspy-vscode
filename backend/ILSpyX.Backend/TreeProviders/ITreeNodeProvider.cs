using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public interface ITreeNodeProvider
{
    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata) => Task.FromResult(Enumerable.Empty<Node>());
    Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage);
}

public class DummyTreeNodeProvider : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return Task.FromResult(DecompileResult.Empty());
    }
}
