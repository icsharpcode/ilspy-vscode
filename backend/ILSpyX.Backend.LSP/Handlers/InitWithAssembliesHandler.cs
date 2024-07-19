﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Application;
using ILSpy.Backend.Model;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/initWithAssemblies", Direction.ClientToServer)]
public class InitWithAssembliesHandler : IJsonRpcRequestHandler<InitWithAssembliesRequest, InitWithAssembliesResponse>
{
    private readonly ILSpyXApplication application;

    public InitWithAssembliesHandler(ILSpyXApplication application)
    {
        this.application = application;
    }

    public async Task<InitWithAssembliesResponse> Handle(InitWithAssembliesRequest request, CancellationToken cancellationToken)
    {
        var loadedAssemblyDatas = new List<AssemblyData>();
        foreach (var assemblyPath in request.AssemblyPaths)
        {
            var assemblyData = await application.DecompilerBackend.AddAssemblyAsync(assemblyPath);
            if (assemblyData is not null)
            {
                loadedAssemblyDatas.Add(assemblyData);
            }
        }

        return new InitWithAssembliesResponse(LoadedAssemblies: loadedAssemblyDatas.ToArray());
    }
}
