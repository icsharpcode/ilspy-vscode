// Copyright (c) 2025 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.LSP.Protocol;
using ILSpyX.Backend.Model;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.LSP.Handlers;

[Serial, Method("ilspy/exportNode", Direction.ClientToServer)]
public class ExportNodeHandler(ExportBackend exportBackend, DecompilerBackend decompilerBackend)
    : IJsonRpcRequestHandler<ExportNodeRequest, ExportNodeResponse>
{
    public async Task<ExportNodeResponse> Handle(
        ExportNodeRequest request,
        CancellationToken cancellationToken)
    {
        // In SingleFile mode, OutputTarget carries the target file path.
        (var result, bool shouldUpdateAssemblyList) =
            await decompilerBackend.DetectAutoLoadedAssemblies(() =>
                request.ExportOutputType == ExportOutputType.SingleFile
                    ? exportBackend.ExportNodeToSingleFileAsync(
                        request.NodeMetadata,
                        request.OutputLanguage,
                        request.OutputTarget,
                        request.IncludeCompilerGenerated,
                        cancellationToken)
                    : exportBackend.ExportNodeAsync(
                        request.NodeMetadata,
                        request.OutputLanguage,
                        request.OutputTarget,
                        request.IncludeCompilerGenerated,
                        cancellationToken));

        return new ExportNodeResponse(
            result.Succeeded,
            result.OutputTarget,
            result.FilesWritten,
            result.ErrorCount,
            result.ErrorMessage,
            shouldUpdateAssemblyList);
    }
}