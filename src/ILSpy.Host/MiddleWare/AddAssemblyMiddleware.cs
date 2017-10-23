// See the LICENSE file in the project root for more information.

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

        public override string EndPoint => MsilDecompilerEndpoints.AddAssembly;

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
