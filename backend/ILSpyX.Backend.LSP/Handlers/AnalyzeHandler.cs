// Copyright (c) 2024 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Application;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/analyze", Direction.ClientToServer)]
public class AnalyzeHandler : IJsonRpcRequestHandler<AnalyzeRequest, AnalyzeResponse>
{
    private readonly ILSpyXApplication application;

    public AnalyzeHandler(ILSpyXApplication application)
    {
        this.application = application;
    }

    public async Task<AnalyzeResponse> Handle(AnalyzeRequest request, CancellationToken cancellationToken)
    {
        var resultNodes = await application.TreeNodeProviders.AnalyzersRoot.GetChildrenAsync(request.NodeMetadata);
        return new AnalyzeResponse(resultNodes);
    }
}
