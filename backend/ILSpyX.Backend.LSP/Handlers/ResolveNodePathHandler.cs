// Copyright (c) ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.


using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.TreeProviders;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/resolveNodePath", Direction.ClientToServer)]
public class ResolveNodePathHandler(DecompilerBackend decompilerBackend, TreePathResolver treePathResolver)
    : IJsonRpcRequestHandler<ResolveNodePathRequest, ResolveNodePathResponse>
{
    public async Task<ResolveNodePathResponse> Handle(ResolveNodePathRequest request,
        CancellationToken cancellationToken)
    {
        (var nodes, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(async () =>
                await treePathResolver.ResolveNodePathAsync(request.NodeMetadata));
        return new ResolveNodePathResponse(nodes, request.NodeMetadata, shouldUpdateAssemblyList);
    }
}