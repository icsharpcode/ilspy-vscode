// Copyright (c) 2024 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/analyze", Direction.ClientToServer)]
public class AnalyzeHandler : IJsonRpcRequestHandler<AnalyzeRequest, AnalyzeResponse>
{
    private readonly AnalyzerBackend analyzerBackend;

    public AnalyzeHandler(AnalyzerBackend searchBackend)
    {
        this.analyzerBackend = searchBackend;
    }

    public Task<AnalyzeResponse> Handle(AnalyzeRequest request, CancellationToken cancellationToken)
    {
        var resultNodes = analyzerBackend.Analyze(request.NodeMetadata);
        return Task.FromResult(new AnalyzeResponse(resultNodes));
    }
}
