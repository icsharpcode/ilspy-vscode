// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Http;

namespace ILSpy.Host
{
    public class RemoveAssemblyMiddleware : BaseMiddleware
    {
        public RemoveAssemblyMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.RemoveAssembly;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<RemoveAssemblyRequest>();
            var result = _decompilationProvider.RemoveAssembly(requestObject.AssemblyPath);
            return new RemoveAssemblyResponse { Removed = result };
        }
    }
}
