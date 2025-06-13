// Copyright (c) 2023 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.


using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.TreeProviders;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/decompileNode", Direction.ClientToServer)]
public class DecompileNodeHandler(DecompilerBackend decompilerBackend, TreeNodeProviders treeNodeProviders)
    : IJsonRpcRequestHandler<DecompileNodeRequest, DecompileResponse>
{
    public async Task<DecompileResponse> Handle(DecompileNodeRequest request, CancellationToken cancellationToken)
    {
        (var result, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                Task.FromResult(treeNodeProviders
                    .ForNode(request.NodeMetadata)
                    .Decompile(request.NodeMetadata, request.OutputLanguage)));
        return new DecompileResponse(result, shouldUpdateAssemblyList);
    }

}
