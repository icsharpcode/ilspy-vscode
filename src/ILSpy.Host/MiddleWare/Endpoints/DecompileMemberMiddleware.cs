// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;
using Mono.Cecil;

namespace ILSpy.Host
{
    public class DecompileMemberMiddleware : BaseMiddleware
    {
        public DecompileMemberMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndPoint => MsilDecompilerEndpoints.DecompileMember;

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue)
            {
                var endpoint = httpContext.Request.Path.Value;
                if (endpoint == EndPoint)
                {
                    DecompileMemberRequest requestData = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                        .ToObject<DecompileMemberRequest>();

                    var members = _decompilationProvider.GetChildren(requestData.AssemblyPath, TokenType.TypeDef, requestData.TypeRid);
                    foreach (var member in members)
                    {
                        if (member.Token.RID == requestData.MemberRid
                            && member.Token.TokenType == (TokenType)requestData.MemberType)
                        {
                            await Task.Run(() =>
                            {
                                var code = new DecompileCode { Decompiled = _decompilationProvider.GetMemberCode(requestData.AssemblyPath, member.Token) };
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
