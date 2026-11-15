// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Navigation;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/findDefinitions", Direction.ClientToServer)]
public class FindDefinitionsHandler(DecompilerBackend decompilerBackend, SymbolNavigationBackend navigationBackend)
    : IJsonRpcRequestHandler<FindDefinitionsRequest, CodeLocationsResponse>
{
    public async Task<CodeLocationsResponse> Handle(FindDefinitionsRequest request,
        CancellationToken cancellationToken)
    {
        (var locations, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                navigationBackend.FindDefinitions(request.NodeMetadata, request.OutputLanguage, request.Line,
                    request.Column));
        return new CodeLocationsResponse(locations, shouldUpdateAssemblyList);
    }
}

[Serial, Method("ilspy/findImplementations", Direction.ClientToServer)]
public class FindImplementationsHandler(
    DecompilerBackend decompilerBackend,
    SymbolNavigationBackend navigationBackend)
    : IJsonRpcRequestHandler<FindImplementationsRequest, CodeLocationsResponse>
{
    public async Task<CodeLocationsResponse> Handle(FindImplementationsRequest request,
        CancellationToken cancellationToken)
    {
        (var locations, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                navigationBackend.FindImplementations(request.NodeMetadata, request.OutputLanguage, request.Line,
                    request.Column, cancellationToken));
        return new CodeLocationsResponse(locations, shouldUpdateAssemblyList);
    }
}
