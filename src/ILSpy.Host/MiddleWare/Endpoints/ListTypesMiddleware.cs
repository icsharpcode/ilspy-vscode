// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Http;

namespace ILSpy.Host
{
    public class ListTypesMiddleware : BaseMiddleware
    {
        public ListTypesMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.ListTypes;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<ListTypesRequest>();
            var types = _decompilationProvider.ListTypes(requestObject.AssemblyPath, requestObject.Namespace);
            var data = new ListTypesResponse { Types = types };
            return data;
        }
    }
}
