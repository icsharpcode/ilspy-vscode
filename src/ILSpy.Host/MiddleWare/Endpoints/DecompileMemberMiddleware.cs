// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;
using Mono.Cecil;

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

            var members = _decompilationProvider.GetChildren(requestData.AssemblyPath, TokenType.TypeDef, requestData.TypeRid);
            foreach (var member in members)
            {
                if (member.Token.RID == requestData.MemberRid
                    && member.Token.TokenType == (TokenType)requestData.MemberType)
                {
                    var code = new DecompileCode { Decompiled = _decompilationProvider.GetMemberCode(requestData.AssemblyPath, member.Token) };
                    return code;
                }
            }

            var message = $"Error: could not find member matching (type: {requestData.TypeRid}, member: {((TokenType)requestData.MemberType).ToString()}:{requestData.MemberRid}).";
            return message;
        }
    }
}
