// Copyright (c) 2024 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.TreeProviders;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/analyze", Direction.ClientToServer)]
public class AnalyzeHandler(DecompilerBackend decompilerBackend, AnalyzersRootNodesProvider analyzersRootNodesProvider)
    : IJsonRpcRequestHandler<AnalyzeRequest, AnalyzeResponse>
{
    public async Task<AnalyzeResponse> Handle(AnalyzeRequest request, CancellationToken cancellationToken)
    {
        (var resultNodes, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                analyzersRootNodesProvider.GetChildrenAsync(request.NodeMetadata));
        return new AnalyzeResponse(resultNodes, shouldUpdateAssemblyList);
    }
}
