// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;
using System.Threading.Tasks;

namespace ILSpy.Host
{
    public abstract class BaseMiddleware
    {
        protected readonly RequestDelegate _next;
        protected readonly IDecompilationProvider _decompilationProvider;

        public abstract string EndpointName { get; }

        public BaseMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
        {
            _next = next;
            _decompilationProvider = decompilationProvider;
        }

        public abstract object Handle(HttpContext httpContext);
    }
}
