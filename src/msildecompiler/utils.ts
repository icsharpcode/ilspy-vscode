'use strict';

import { MsilDecompilerServer } from './server';
import * as protocol from './protocol';

export function addAssembly(server: MsilDecompilerServer, request: protocol.AddAssemblyRequest) {
    return server.makeRequest<protocol.AddAssemblyResponse>(protocol.Requests.AddAssembly, request);
}

export function decompileAssembly(server: MsilDecompilerServer, request: protocol.DecompileAssemblyRequest) {
    return server.makeRequest<protocol.DecompileResponse>(protocol.Requests.DecompileAssembly, request);
}

export function getTypes(server: MsilDecompilerServer, request: protocol.ListTypesRequest) {
    return server.makeRequest<protocol.ListTypesResponse>(protocol.Requests.ListTypes, request);
}

export function decompileType(server: MsilDecompilerServer, request: protocol.DecompileTypeRequest) {
    return server.makeRequest<protocol.DecompileResponse>(protocol.Requests.DecopmileType, request);
}

export function getMembers(server: MsilDecompilerServer, request: protocol.ListMembersRequest) {
    return server.makeRequest<protocol.ListMembersResponse>(protocol.Requests.ListMembers, request);
}

export function decompileMember(server: MsilDecompilerServer, request: protocol.DecompileMemberRequest) {
    return server.makeRequest<protocol.DecompileResponse>(protocol.Requests.DecompileMember, request);
}
