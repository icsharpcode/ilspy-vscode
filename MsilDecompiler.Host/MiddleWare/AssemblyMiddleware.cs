using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MsilDecompiler.Host;
using MsilDecompiler.Host.Providers;
using Mono.Cecil;

namespace MsilDecompiler.Host
{
    public class AssemblyMiddleware
    {
        private readonly IDecompilationProvider _decompilationProvider;

        public AssemblyMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
        {
            _decompilationProvider = decompilationProvider;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == MsilDecompilerEndpoints.Assembly)
                {
                    await Task.Run(() =>
                    {
                        var code = _decompilationProvider.GetCode(TokenType.Assembly, 0);
                        MiddlewareHelpers.WriteTo(httpContext.Response, code);
                    });
                }
            }
        }
    }
}
