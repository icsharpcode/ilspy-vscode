using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MsilDecompiler.Host.Providers;
using System.Linq;

namespace MsilDecompiler.Host
{
    public class GetTypesMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDecompilationProvider _decompilationProvider;

        public GetTypesMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
        {
            _next = next;
            _decompilationProvider = decompilationProvider;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == MsilDecompilerEndpoints.ListTypes)
                {
                    await Task.Run(() =>
                    {
                        var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                            .ToObject<GetTypesRequest>();
                        var assemblyPath = requestObject.AssemblyPath;
                        var types = _decompilationProvider.GetTypeTuples(assemblyPath);
                        var data = new { Types = types.Select<global::System.Tuple<string, global::Mono.Cecil.MetadataToken>, global::MsilDecompiler.Host.MemberData>(tuple => new global::MsilDecompiler.Host.MemberData { Name = tuple.Item1, Token = tuple.Item2 }) };
                        MiddlewareHelpers.WriteTo(httpContext.Response, data);
                    });
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}
