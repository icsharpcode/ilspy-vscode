// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ILSpy.Host
{
    public class StopServerMiddleware
    {
        private readonly IApplicationLifetime _lifetime;

        public StopServerMiddleware(RequestDelegate next, IApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == MsilDecompilerEndpoints.StopServer)
                {
                    await Task.Run(() =>
                    {
                        Thread.Sleep(200);
                        _lifetime.StopApplication();
                    });
                }
            }
        }
    }
}
