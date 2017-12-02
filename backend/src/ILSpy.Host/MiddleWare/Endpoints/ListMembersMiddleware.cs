// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Http;
using Mono.Cecil;

namespace ILSpy.Host
{
    public class ListMembersMiddleware : BaseMiddleware
    {
        public ListMembersMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.ListMembers;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<ListMembersRequest>();
            var members = _decompilationProvider.GetChildren(requestObject.AssemblyPath, TokenType.TypeDef, requestObject.Rid);
            var data = new  ListMembersResponse { Members = members };
            return data;
        }
    }
}
