// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Http;

namespace ILSpy.Host.MiddleWare.Endpoints
{
    public class ListNamespacesMiddleware : BaseMiddleware
    {
        public override string EndpointName => MsilDecompilerEndpoints.ListNamespaces;

        public ListNamespacesMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<ListNamespacesRequest>();
            var assemblyPath = requestObject.AssemblyPath;
            var types = _decompilationProvider.ListNamespaces(assemblyPath);
            var data = new ListNamespacesResponse { Namespaces = types };
            return data;
        }
    }
}
