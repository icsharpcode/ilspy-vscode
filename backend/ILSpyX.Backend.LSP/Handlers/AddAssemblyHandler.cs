// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Application;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Search;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/addAssembly", Direction.ClientToServer)]
public class AddAssemblyHandler : IJsonRpcRequestHandler<AddAssemblyRequest, AddAssemblyResponse>
{
    private readonly ILSpyXApplication application;
    private readonly SearchBackend searchBackend;

    public AddAssemblyHandler(ILSpyXApplication application, SearchBackend searchBackend)
    {
        this.application = application;
        this.searchBackend = searchBackend;
    }

    public async Task<AddAssemblyResponse> Handle(AddAssemblyRequest request, CancellationToken cancellationToken)
    {
        if (request.AssemblyPath != null)
        {
            await searchBackend.AddAssembly(request.AssemblyPath);
        }

        var result = request.AssemblyPath != null ? application.DecompilerBackend.AddAssembly(request.AssemblyPath) : null;
        return new AddAssemblyResponse(Added: result != null, AssemblyData: result);
    }
}
