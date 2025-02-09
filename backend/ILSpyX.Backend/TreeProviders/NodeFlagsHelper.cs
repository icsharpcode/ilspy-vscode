using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Search;
using ILSpyX.Backend.Model;

namespace ILSpyX.Backend.TreeProviders;

public static class NodeFlagsHelper
{
    public static NodeFlags GetNodeFlags(AssemblyData assemblyData)
    {
        return assemblyData.IsAutoLoaded ? NodeFlags.AutoLoaded : NodeFlags.None;
    }

    public static NodeFlags GetNodeFlags(IEntity entity)
    {
        return entity.IsCompilerGenerated() ? NodeFlags.CompilerGenerated : NodeFlags.None;
    }

    public static NodeFlags GetNodeFlags(SearchResult searchResult)
    {
        if (searchResult is MemberSearchResult memberSearchResult)
        {
            return GetNodeFlags(memberSearchResult.Member);
        }

        return NodeFlags.None;
    }
}