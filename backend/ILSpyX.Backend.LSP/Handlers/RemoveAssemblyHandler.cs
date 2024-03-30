// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Application;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Search;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/removeAssembly", Direction.ClientToServer)]
public class RemoveAssemblyHandler : IJsonRpcRequestHandler<RemoveAssemblyRequest, RemoveAssemblyResponse>
{
    private readonly ILSpyXApplication application;
    private readonly SearchBackend searchBackend;

    public RemoveAssemblyHandler(ILSpyXApplication application, SearchBackend searchBackend)
    {
        this.application = application;
        this.searchBackend = searchBackend;
    }

    public async Task<RemoveAssemblyResponse> Handle(RemoveAssemblyRequest request, CancellationToken cancellationToken)
    {
        if (request.AssemblyPath != null)
        {
            await searchBackend.RemoveAssembly(request.AssemblyPath);
        }

        bool result = request.AssemblyPath != null && application.DecompilerBackend.RemoveAssembly(request.AssemblyPath);
        return new RemoveAssemblyResponse(Removed: result);
    }
}
