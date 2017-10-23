// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;
using Mono.Cecil;

namespace ILSpy.Host
{
    public class DecompileAssemblyMiddleware : BaseMiddleware
    {
        public DecompileAssemblyMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndPoint => MsilDecompilerEndpoints.DecompileAssembly;

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == EndPoint)
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
