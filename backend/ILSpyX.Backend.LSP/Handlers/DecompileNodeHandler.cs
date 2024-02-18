// Copyright (c) 2023 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.


using ILSpy.Backend.Application;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/decompileNode", Direction.ClientToServer)]
public class DecompileNodeHandler : IJsonRpcRequestHandler<DecompileNodeRequest, DecompileResponse>
{
    private readonly ILSpyXApplication application;

    public DecompileNodeHandler(ILSpyXApplication application)
    {
        this.application = application;
    }

    public Task<DecompileResponse> Handle(DecompileNodeRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new DecompileResponse(
            application.TreeNodeProviders
                .ForNode(request.NodeMetadata)
                .Decompile(request.NodeMetadata, request.OutputLanguage)));
    }

}
