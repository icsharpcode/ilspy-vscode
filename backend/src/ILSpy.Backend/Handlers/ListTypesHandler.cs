// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpy.Backend.Handlers
{
    [Serial, Method("ilspy/listTypes", Direction.ClientToServer)]
    public class ListTypesHandler : IJsonRpcRequestHandler<ListTypesRequest, ListTypesResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;

        public ListTypesHandler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public Task<ListTypesResponse> Handle(ListTypesRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new ListTypesResponse(decompilerBackend.ListTypes(request.AssemblyPath, request.Namespace)));
        }
    }
}
