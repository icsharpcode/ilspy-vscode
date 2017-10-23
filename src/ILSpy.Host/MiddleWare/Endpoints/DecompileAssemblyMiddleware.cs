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

        public override string EndpointName => MsilDecompilerEndpoints.DecompileAssembly;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<DecompileAssemblyRequest>();
            var code = new DecompileCode { Decompiled = _decompilationProvider.GetCode(requestObject.AssemblyPath, TokenType.Assembly, 0) };
            return code;
        }
    }
}
