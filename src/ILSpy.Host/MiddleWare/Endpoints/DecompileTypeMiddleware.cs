// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;
using System.Linq;
using Mono.Cecil;

namespace ILSpy.Host
{
    public class DecompileTypeMiddleware : BaseMiddleware
    {
        public DecompileTypeMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndPoint => MsilDecompilerEndpoints.DecompileType;

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
                            .ToObject<DecompileTypeRequest>();
                        var code = new DecompileCode
                        {
                            Decompiled = _decompilationProvider.GetCode(requestObject.AssemblyPath, TokenType.TypeDef, requestObject.Rid)
                        };
                        MiddlewareHelpers.WriteTo(httpContext.Response, code);
                    });
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}