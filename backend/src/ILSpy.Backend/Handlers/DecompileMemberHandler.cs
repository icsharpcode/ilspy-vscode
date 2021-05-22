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
    [Serial, Method("ilspy/decompileMember", Direction.ClientToServer)]
    public class DecompileMemberHandler : IJsonRpcRequestHandler<DecompileMemberRequest, DecompileResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;

        public DecompileMemberHandler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public Task<DecompileResponse> Handle(DecompileMemberRequest request, CancellationToken cancellationToken)
        {
            if (request.AssemblyPath == null)
            {
                return Task.FromResult(
                    new DecompileResponse(
                        IsError: true,
                        ErrorMessage: $"Error: Invalid assembly path."));
            }

            var members = decompilerBackend.GetMembers(request.AssemblyPath, MetadataTokens.TypeDefinitionHandle(request.Type));
            var requestedMember = MetadataTokens.EntityHandle(request.Member);
            foreach (var member in members)
            {
                var memberToken = MetadataTokens.EntityHandle(member.Token);
                if (memberToken == requestedMember)
                {
                    return Task.FromResult(
                        new DecompileResponse(decompilerBackend.GetCode(request.AssemblyPath, memberToken)));
                }
            }

            return Task.FromResult(
                new DecompileResponse(
                    IsError: true,
                    ErrorMessage: $"Error: could not find member matching (type: {request.Type:8x}, member: {request.Member:8x})."));
        }
    }
}
