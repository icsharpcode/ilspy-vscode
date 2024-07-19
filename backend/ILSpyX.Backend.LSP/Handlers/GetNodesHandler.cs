// Copyright (c) 2023 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.


using ILSpy.Backend.Application;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/getNodes", Direction.ClientToServer)]
public class GetNodesHandler : IJsonRpcRequestHandler<GetNodesRequest, GetNodesResponse>
{
    private readonly ILSpyXApplication application;

    public GetNodesHandler(ILSpyXApplication application)
    {
        this.application = application;
    }

    public async Task<GetNodesResponse> Handle(GetNodesRequest request, CancellationToken cancellationToken)
    {
        return new GetNodesResponse(
            await application.TreeNodeProviders.ForNode(request.NodeMetadata).GetChildrenAsync(request.NodeMetadata));
    }

}
