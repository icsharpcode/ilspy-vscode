// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Application;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/removeAssembly", Direction.ClientToServer)]
public class RemoveAssemblyHandler(ILSpyXApplication application)
    : IJsonRpcRequestHandler<RemoveAssemblyRequest, RemoveAssemblyResponse>
{
    public async Task<RemoveAssemblyResponse> Handle(RemoveAssemblyRequest request, CancellationToken cancellationToken)
    {
        bool result = request.AssemblyPath != null &&
                      await application.DecompilerBackend.RemoveAssemblyAsync(request.AssemblyPath);
        return new RemoveAssemblyResponse(Removed: result);
    }
}
