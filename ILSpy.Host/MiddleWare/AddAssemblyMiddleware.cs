using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MsilDecompiler.Host.Providers;

namespace MsilDecompiler.Host
{
    public class AddAssemblyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDecompilationProvider _decompilationProvider;

        public AddAssemblyMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
        {
            _next = next;
            _decompilationProvider = decompilationProvider;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == MsilDecompilerEndpoints.AddAssembly)
                {
                    await Task.Run(() =>
                    {
                        var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                            .ToObject<AddAssemblyRequest>();
                        var result = _decompilationProvider.AddAssembly(requestObject.AssemblyPath);
                        MiddlewareHelpers.WriteTo(httpContext.Response, new { Added = result });
                    });
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}
