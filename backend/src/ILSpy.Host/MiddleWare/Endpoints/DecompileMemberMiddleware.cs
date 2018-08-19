// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata.Ecma335;

namespace ILSpy.Host
{
    public class DecompileMemberMiddleware : BaseMiddleware
    {
        public DecompileMemberMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.DecompileMember;

        public override object Handle(HttpContext httpContext)
        {
            DecompileMemberRequest requestData = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<DecompileMemberRequest>();

            var members = _decompilationProvider.GetMembers(requestData.AssemblyPath, MetadataTokens.TypeDefinitionHandle(requestData.Type));
            var requestedMember = MetadataTokens.EntityHandle(requestData.Member);
            foreach (var member in members)
            {
                var memberToken = MetadataTokens.EntityHandle(member.Token);
                if (memberToken == requestedMember)
                {
                    var code = new DecompileCode { Decompiled = _decompilationProvider.GetCode(requestData.AssemblyPath, memberToken) };
                    return code;
                }
            }

            var message = $"Error: could not find member matching (type: {requestData.Type:8x}, member: {requestData.Member:8x}).";
            return message;
        }
    }
}
