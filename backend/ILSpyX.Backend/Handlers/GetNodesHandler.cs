// Copyright (c) 2023 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Backend.Handlers;

using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

[Serial, Method("ilspy/getNodes", Direction.ClientToServer)]
public class GetNodesHandler : IJsonRpcRequestHandler<GetNodesRequest, GetNodesResponse>
{
    private readonly ILSpyXApplication application;

    public GetNodesHandler(ILSpyXApplication application)
    {
        this.application = application;
    }

    public Task<GetNodesResponse> Handle(GetNodesRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GetNodesResponse(
            application.TreeNodeProviders.ForNode(request.NodeMetadata).GetChildren(request.NodeMetadata)));
    }

}
