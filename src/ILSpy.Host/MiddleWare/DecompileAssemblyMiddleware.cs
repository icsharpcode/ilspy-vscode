using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MsilDecompiler.Host.Providers;
using Mono.Cecil;

namespace MsilDecompiler.Host
{
    public class DecompileAssemblyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDecompilationProvider _decompilationProvider;

        public DecompileAssemblyMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
        {
            _next = next;
            _decompilationProvider = decompilationProvider;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == MsilDecompilerEndpoints.DecompileAssembly)
                {
                    await Task.Run(() =>
                    {
                        var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                            .ToObject<DecompileAssemblyRequest>();
                        var code = new DecompileCode { Decompiled = _decompilationProvider.GetCode(requestObject.AssemblyPath, TokenType.Assembly, 0) };
                        MiddlewareHelpers.WriteTo(httpContext.Response, code);
                    });
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}
