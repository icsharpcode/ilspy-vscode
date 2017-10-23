// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;
using System.Linq;

namespace ILSpy.Host
{
    public class ListTypesMiddleware : BaseMiddleware
    {
        public ListTypesMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndPoint => MsilDecompilerEndpoints.ListTypes;

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
                            .ToObject<ListTypesRequest>();
                        var assemblyPath = requestObject.AssemblyPath;
                        var types = _decompilationProvider.ListTypes(assemblyPath);
                        var data = new ListTypesResponse { Types = types };
                        MiddlewareHelpers.WriteTo(httpContext.Response, data);
                    });
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}
