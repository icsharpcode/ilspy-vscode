// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Search;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/search", Direction.ClientToServer)]
public class SearchHandler(DecompilerBackend decompilerBackend, SearchBackend searchBackend)
    : IJsonRpcRequestHandler<SearchRequest, SearchResponse>
{
    public async Task<SearchResponse> Handle(SearchRequest request, CancellationToken cancellationToken)
    {
        (var resultNodes, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                searchBackend.Search(request.Term, cancellationToken));
        return new SearchResponse(resultNodes, shouldUpdateAssemblyList);
    }
}
