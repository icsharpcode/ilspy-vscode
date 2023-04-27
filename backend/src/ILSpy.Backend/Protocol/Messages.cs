// Copyright (c) 2021 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Backend.Model;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using System.Collections.Generic;

namespace ILSpy.Backend.Protocol
{
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

    #region decompileAssembly, decompileMember, decompileType

    [Serial, Method("ilspy/decompileAssembly", Direction.ClientToServer)]
    public record DecompileAssemblyRequest(string? AssemblyPath)
        : IRequest<DecompileResponse>;

    [Serial, Method("ilspy/decompileMember", Direction.ClientToServer)]
    public record DecompileMemberRequest(
        string? AssemblyPath,
        int Type,
        int Member
    ) : IRequest<DecompileResponse>;

    [Serial, Method("ilspy/decompileType", Direction.ClientToServer)]
    public record DecompileTypeRequest(string? AssemblyPath, int Handle)
        : IRequest<DecompileResponse>;

    [Serial, Method("ilspy/decompileNode", Direction.ClientToServer)]
    public record DecompileNodeRequest(NodeMetadata NodeMetadata)
        : IRequest<DecompileResponse>;

    public record DecompileResponse(
        IDictionary<string, string>? DecompiledCode,
        bool IsError,
        string? ErrorMessage
    )
    {
        public DecompileResponse(IDictionary<string, string>? DecompiledCode) : this(DecompiledCode, false, null) { }

        public DecompileResponse(bool IsError, string? ErrorMessage) : this(null, IsError, ErrorMessage) { }
    }

    #endregion

    #region getNodes

    [Serial, Method("ilspy/getNodes", Direction.ClientToServer)]
    public record GetNodesRequest(NodeMetadata NodeMetadata)
        : IRequest<GetNodesResponse>;

    public record GetNodesResponse(IEnumerable<Node>? Nodes);

    #endregion

    #region listMembers

    [Serial, Method("ilspy/listMembers", Direction.ClientToServer)]
    public record ListMembersRequest(
        string? AssemblyPath,
        int Handle
    ) : IRequest<ListMembersResponse>;

    public record ListMembersResponse(IEnumerable<MemberData>? Members);

    #endregion

    #region listNamespaces

    [Serial, Method("ilspy/listNamespaces", Direction.ClientToServer)]
    public record ListNamespacesRequest(string? AssemblyPath)
        : IRequest<ListNamespacesResponse>;

    public record ListNamespacesResponse(IEnumerable<string>? Namespaces);

    #endregion

    #region listAssemblyReferences

    [Serial, Method("ilspy/listAssemblyReferences", Direction.ClientToServer)]
    public record ListAssemblyReferencesRequest(string? AssemblyPath)
        : IRequest<ListAssemblyReferencesResponse>;

    public record ListAssemblyReferencesResponse(IEnumerable<string>? References);

    #endregion

    #region listTypes

    [Serial, Method("ilspy/listTypes", Direction.ClientToServer)]
    public record ListTypesRequest(string? AssemblyPath, string? Namespace)
        : IRequest<ListTypesResponse>;

    public record ListTypesResponse(IEnumerable<MemberData>? Types);

    #endregion

    #region search

    [Serial, Method("ilspy/search", Direction.ClientToServer)]
    public record SearchRequest(string Term)
        : IRequest<SearchResponse>;

    public record SearchResponse(IEnumerable<Node>? Results);

    #endregion
}
