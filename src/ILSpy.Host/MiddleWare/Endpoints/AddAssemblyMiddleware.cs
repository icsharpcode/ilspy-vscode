// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;

namespace ILSpy.Host
{
    public class AddAssemblyMiddleware : BaseMiddleware
    {
        public AddAssemblyMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.AddAssembly;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<AddAssemblyRequest>();
            var result = _decompilationProvider.AddAssembly(requestObject.AssemblyPath);
            return new AddAssemblyResponse { Added = result };
        }
    }
}
