// Copyright (c) 2021 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using System.Collections.Generic;

namespace ILSpyX.Backend.LSP.Protocol
{
    #region initWithAssemblies

    [Serial, Method("ilspy/initWithAssemblies", Direction.ClientToServer)]
    public record InitWithAssembliesRequest(string[] AssemblyPaths)
        : IRequest<InitWithAssembliesResponse>;

    public record InitWithAssembliesResponse(AssemblyData[]? LoadedAssemblies);

    #endregion

    #region addAssembly

    [Serial, Method("ilspy/addAssembly", Direction.ClientToServer)]
    public record AddAssemblyRequest(string? AssemblyPath)
        : IRequest<AddAssemblyResponse>;

    public record AddAssemblyResponse(bool Added, AssemblyData? AssemblyData);

    #endregion

    #region removeAssembly

    [Serial, Method("ilspy/removeAssembly", Direction.ClientToServer)]
    public record RemoveAssemblyRequest(string? AssemblyPath)
        : IRequest<RemoveAssemblyResponse>;

    public record RemoveAssemblyResponse(bool Removed);

    #endregion

    #region decompileNode

    [Serial, Method("ilspy/decompileNode", Direction.ClientToServer)]
    public record DecompileNodeRequest(NodeMetadata NodeMetadata, string OutputLanguage)
        : IRequest<DecompileResponse>;

    public record DecompileResponse(
        string? DecompiledCode,
        bool IsError,
        string? ErrorMessage
    )
    {
        public DecompileResponse(DecompileResult decompileResult) :
            this(decompileResult.DecompiledCode, decompileResult.IsError, decompileResult.ErrorMessage)
        { }
    }

    #endregion

    #region getNodes

    [Serial, Method("ilspy/getNodes", Direction.ClientToServer)]
    public record GetNodesRequest(NodeMetadata? NodeMetadata)
        : IRequest<GetNodesResponse>;

    public record GetNodesResponse(IEnumerable<Node>? Nodes);

    #endregion

    #region search

    [Serial, Method("ilspy/search", Direction.ClientToServer)]
    public record SearchRequest(string Term)
        : IRequest<SearchResponse>;

    public record SearchResponse(IEnumerable<Node>? Results);

    #endregion

    #region analyze

    [Serial, Method("ilspy/analyze", Direction.ClientToServer)]
    public record AnalyzeRequest(NodeMetadata? NodeMetadata)
        : IRequest<AnalyzeResponse>;

    public record AnalyzeResponse(IEnumerable<Node>? Results);

    #endregion
}
