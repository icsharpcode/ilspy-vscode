// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpy.Backend.Handlers
{
    [Serial, Method("ilspy/addAssembly", Direction.ClientToServer)]
    public class AddAssemblyHandler : IJsonRpcRequestHandler<AddAssemblyRequest, AddAssemblyResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;

        public AddAssemblyHandler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public Task<AddAssemblyResponse> Handle(AddAssemblyRequest request, CancellationToken cancellationToken)
        {
            var result = request.AssemblyPath != null && decompilerBackend.AddAssembly(request.AssemblyPath);
            return Task.FromResult(new AddAssemblyResponse(Added: result));
        }
    }
}
