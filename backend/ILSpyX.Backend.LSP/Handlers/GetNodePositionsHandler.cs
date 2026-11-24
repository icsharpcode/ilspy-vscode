// Copyright (c) ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.


using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/getNodePositions", Direction.ClientToServer)]
public class GetNodePositionsHandler(DecompilerBackend decompilerBackend)
    : IJsonRpcRequestHandler<GetNodePositionsRequest, GetNodePositionsResponse>
{
    public async Task<GetNodePositionsResponse> Handle(GetNodePositionsRequest request,
        CancellationToken cancellationToken)
    {
        (var positions, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                decompilerBackend.GetNodePositions(
                    request.NodeMetadata.GetAssemblyFileIdentifier(),
                    MetadataTokens.EntityHandle(request.NodeMetadata.SymbolToken),
                    request.OutputLanguage));
        return new GetNodePositionsResponse(positions, shouldUpdateAssemblyList);
    }
}