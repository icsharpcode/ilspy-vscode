// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Application;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Search;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/addAssembly", Direction.ClientToServer)]
public class AddAssemblyHandler(ILSpyXApplication application)
    : IJsonRpcRequestHandler<AddAssemblyRequest, AddAssemblyResponse>
{
    public async Task<AddAssemblyResponse> Handle(AddAssemblyRequest request, CancellationToken cancellationToken)
    {
        var result = request.AssemblyPath != null ? await application.DecompilerBackend.AddAssemblyAsync(request.AssemblyPath) : null;
        return new AddAssemblyResponse(Added: result != null, AssemblyData: result);
    }
}
