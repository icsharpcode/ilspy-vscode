// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpy.Backend.Handlers
{
    [Serial, Method("ilspy/decompileAssembly", Direction.ClientToServer)]
    public class DecompileAssemblyHandler : IJsonRpcRequestHandler<DecompileAssemblyRequest, DecompileResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;

        public DecompileAssemblyHandler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public Task<DecompileResponse> Handle(DecompileAssemblyRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new DecompileResponse(decompilerBackend.GetCode(request.AssemblyPath, EntityHandle.AssemblyDefinition)));
        }
    }
}
