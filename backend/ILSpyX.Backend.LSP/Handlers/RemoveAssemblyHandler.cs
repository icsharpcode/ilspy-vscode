// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Application;
using ILSpy.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/removeAssembly", Direction.ClientToServer)]
public class RemoveAssemblyHandler : IJsonRpcRequestHandler<RemoveAssemblyRequest, RemoveAssemblyResponse>
{
    private readonly ILSpyXApplication application;

    public RemoveAssemblyHandler(ILSpyXApplication application)
    {
        this.application = application;
    }

    public Task<RemoveAssemblyResponse> Handle(RemoveAssemblyRequest request, CancellationToken cancellationToken)
    {
        bool result = request.AssemblyPath != null && application.DecompilerBackend.RemoveAssembly(request.AssemblyPath);
        return Task.FromResult(new RemoveAssemblyResponse(Removed: result));
    }
}
