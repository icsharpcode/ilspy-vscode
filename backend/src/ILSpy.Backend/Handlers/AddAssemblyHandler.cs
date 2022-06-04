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
        private readonly SearchBackend searchBackend;

        public AddAssemblyHandler(IDecompilerBackend decompilerBackend, SearchBackend searchBackend)
        {
            this.decompilerBackend = decompilerBackend;
            this.searchBackend = searchBackend;
        }

        public Task<AddAssemblyResponse> Handle(AddAssemblyRequest request, CancellationToken cancellationToken)
        {
            if (request.AssemblyPath != null)
            {
                searchBackend.AddAssembly(request.AssemblyPath);
            }

            var result = request.AssemblyPath != null ? decompilerBackend.AddAssembly(request.AssemblyPath) : null;
            return Task.FromResult(new AddAssemblyResponse(
                Added: result != null,
                AssemblyData: result));
        }
    }
}
