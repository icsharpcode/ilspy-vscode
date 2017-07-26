using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MsilDecompiler.Host.Providers;
using Mono.Cecil;

namespace MsilDecompiler.Host
{
    public class DecompileMemberMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDecompilationProvider _decompilationProvider;

        public DecompileMemberMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
        {
            _next = next;
            _decompilationProvider = decompilationProvider;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == MsilDecompilerEndpoints.Member)
                {
                    DecompileMemberRequest requestData = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                        .ToObject<DecompileMemberRequest>();

                    var members = _decompilationProvider.GetChildren(TokenType.TypeDef, requestData.TypeRid);
                    foreach (var member in members)
                    {
                        if (member.Item2.RID == requestData.MemberRid
                            && member.Item2.TokenType == (TokenType)requestData.MemberType)
                        {
                            await Task.Run(() =>
                            {
                                var code = new DecompileCode { Decompiled = _decompilationProvider.GetMemberCode(member.Item2) };
                                MiddlewareHelpers.WriteTo(httpContext.Response, code);
                            });
                            return;
                        }
                    }

                    await Task.Run(() =>
                    {
                        var message = $"Error: could not find member matching (type: {requestData.TypeRid}, member: {((TokenType)requestData.MemberType).ToString()}:{requestData.MemberRid}).";
                        MiddlewareHelpers.WriteTo(httpContext.Response, message);
                    });
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}
