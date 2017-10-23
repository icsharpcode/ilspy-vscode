// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ILSpy.Host.Providers;
using System.Linq;
using Mono.Cecil;

namespace ILSpy.Host
{
    public class ListMembersMiddleware : BaseMiddleware
    {
        public ListMembersMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.ListMembers;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<ListMembersRequest>();
            var members = _decompilationProvider.GetChildren(requestObject.AssemblyPath, TokenType.TypeDef, requestObject.Rid);
            var data = new  ListMembersResponse { Members = members };
            return data;
        }
    }
}
