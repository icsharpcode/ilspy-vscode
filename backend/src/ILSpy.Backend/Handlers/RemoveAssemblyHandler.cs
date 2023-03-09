﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpy.Backend.Handlers
{
    [Serial, Method("ilspy/removeAssembly", Direction.ClientToServer)]
    public class RemoveAssemblyHandler : IJsonRpcRequestHandler<RemoveAssemblyRequest, RemoveAssemblyResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;
        private readonly SearchBackend searchBackend;

        public RemoveAssemblyHandler(IDecompilerBackend decompilerBackend, SearchBackend searchBackend)
        {
            this.decompilerBackend = decompilerBackend;
            this.searchBackend = searchBackend;
        }

        public async Task<RemoveAssemblyResponse> Handle(RemoveAssemblyRequest request, CancellationToken cancellationToken)
        {
            if (request.AssemblyPath != null)
            {
                await searchBackend.RemoveAssembly(request.AssemblyPath);
            }

            bool result = request.AssemblyPath != null && decompilerBackend.RemoveAssembly(request.AssemblyPath);
            return new RemoveAssemblyResponse(Removed: result);
        }
    }
}
