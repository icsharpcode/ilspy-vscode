// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ILSpy.Host.MiddleWare
{
    public class EndpointMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HashSet<string> _endpoints;
        private readonly IReadOnlyDictionary<string, BaseMiddleware> _endpointHandlers;
        private readonly ILogger _logger;

        public EndpointMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<EndpointMiddleware>();
            var types = typeof(EndpointMiddleware).Assembly.DefinedTypes
                .Where(type => !type.IsAbstract && typeof(BaseMiddleware).GetTypeInfo().IsAssignableFrom(type))
                .Select(t => (BaseMiddleware)Activator.CreateInstance(t, next, decompilationProvider));

            _endpointHandlers = types.ToDictionary(
                    x => x.EndpointName,
                    endpoint => endpoint,
                    StringComparer.OrdinalIgnoreCase
                );

            _endpoints = new HashSet<string>(
                    _endpointHandlers.Keys,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (_endpoints.Contains(endpoint))
                {
                    if (_endpointHandlers.TryGetValue(endpoint, out var handler))
                    {
                        await Task.Run(() =>
                        {
                            var response = handler.Handle(httpContext);
                            MiddlewareHelpers.WriteTo(httpContext.Response, response);
                            return;
                        });
                    }
                }
            }

            await _next(httpContext);
        }
    }

    public static class EndpointMiddlewareExtensions
    {
        public static IApplicationBuilder UseEndpointMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EndpointMiddleware>();
        }
    }
}
