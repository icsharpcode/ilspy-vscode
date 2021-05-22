// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpy.Backend.Handlers
{
    [Serial, Method("ilspy/listNamespaces", Direction.ClientToServer)]
    public class ListNamespacesHandler : IJsonRpcRequestHandler<ListNamespacesRequest, ListNamespacesResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;

        public ListNamespacesHandler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public Task<ListNamespacesResponse> Handle(ListNamespacesRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new ListNamespacesResponse(decompilerBackend.ListNamespaces(request.AssemblyPath)));
        }
    }
}
