// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpy.Backend.Handlers
{
    [Serial, Method("ilspy/listMembers", Direction.ClientToServer)]
    public class ListMembersHandler : IJsonRpcRequestHandler<ListMembersRequest, ListMembersResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;

        public ListMembersHandler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public Task<ListMembersResponse> Handle(ListMembersRequest request, CancellationToken cancellationToken)
        {
            var members = decompilerBackend.GetMembers(request.AssemblyPath, MetadataTokens.TypeDefinitionHandle(request.Handle));
            return Task.FromResult(new ListMembersResponse(members));
        }
    }
}
