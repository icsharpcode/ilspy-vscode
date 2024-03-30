// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Application;
using ILSpy.Backend.Model;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Search;
using OmniSharp.Extensions.JsonRpc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/initWithAssemblies", Direction.ClientToServer)]
public class InitWithAssembliesHandler : IJsonRpcRequestHandler<InitWithAssembliesRequest, InitWithAssembliesResponse>
{
    private readonly ILSpyXApplication application;
    private readonly SearchBackend searchBackend;

    public InitWithAssembliesHandler(ILSpyXApplication application, SearchBackend searchBackend)
    {
        this.application = application;
        this.searchBackend = searchBackend;
    }

    public async Task<InitWithAssembliesResponse> Handle(InitWithAssembliesRequest request, CancellationToken cancellationToken)
    {
        var loadedAssemblyDatas = new List<AssemblyData>();
        foreach (var assemblyPath in request.AssemblyPaths)
        {
            await searchBackend.AddAssembly(assemblyPath);
            var assemblyData = application.DecompilerBackend.AddAssembly(assemblyPath);
            if (assemblyData is not null)
            {
                loadedAssemblyDatas.Add(assemblyData);
            }
        }

        return new InitWithAssembliesResponse(LoadedAssemblies: loadedAssemblyDatas.ToArray());
    }
}
