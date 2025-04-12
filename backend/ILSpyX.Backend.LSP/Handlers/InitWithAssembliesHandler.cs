// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Model;
using OmniSharp.Extensions.JsonRpc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/initWithAssemblies", Direction.ClientToServer)]
public class InitWithAssembliesHandler(DecompilerBackend decompilerBackend)
    : IJsonRpcRequestHandler<InitWithAssembliesRequest, InitWithAssembliesResponse>
{
    public async Task<InitWithAssembliesResponse> Handle(InitWithAssembliesRequest request, CancellationToken cancellationToken)
    {
        var loadedAssemblyDatas = new List<AssemblyData>();
        foreach (string assemblyPath in request.AssemblyPaths)
        {
            var assemblyData = await decompilerBackend.AddAssemblyAsync(assemblyPath);
            if (assemblyData is not null)
            {
                loadedAssemblyDatas.Add(assemblyData);
            }
        }

        return new InitWithAssembliesResponse(LoadedAssemblies: loadedAssemblyDatas.ToArray());
    }
}
