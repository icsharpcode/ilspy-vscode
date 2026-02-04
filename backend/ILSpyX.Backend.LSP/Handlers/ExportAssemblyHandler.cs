// Copyright (c) 2025 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/exportAssembly", Direction.ClientToServer)]
public class ExportAssemblyHandler(DecompilerBackend decompilerBackend)
    : IJsonRpcRequestHandler<ExportAssemblyRequest, ExportAssemblyResponse>
{
    public async Task<ExportAssemblyResponse> Handle(
        ExportAssemblyRequest request,
        CancellationToken cancellationToken)
    {
        (var result, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                decompilerBackend.ExportAssemblyAsync(
                    request.NodeMetadata,
                    request.OutputLanguage,
                    request.OutputDirectory,
                    request.IncludeCompilerGenerated,
                    cancellationToken));

        return new ExportAssemblyResponse(
            result.Succeeded,
            result.OutputDirectory,
            result.FilesWritten,
            result.ErrorCount,
            result.ErrorMessage,
            shouldUpdateAssemblyList);
    }
}
