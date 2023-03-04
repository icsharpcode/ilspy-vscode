using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpy.Backend.Handlers
{
    [Serial, Method("ilspy/listAssemblyReferences", Direction.ClientToServer)]
    public class ListAssemblyReferencesHandler : IJsonRpcRequestHandler<ListAssemblyReferencesRequest, ListAssemblyReferencesResponse>
    {
        private readonly IDecompilerBackend decompilerBackend;

        public ListAssemblyReferencesHandler(IDecompilerBackend decompilerBackend)
        {
            this.decompilerBackend = decompilerBackend;
        }

        public Task<ListAssemblyReferencesResponse> Handle(ListAssemblyReferencesRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new ListAssemblyReferencesResponse(decompilerBackend.ListAssemblyReferences(request.AssemblyPath)));
        }
    }
}
