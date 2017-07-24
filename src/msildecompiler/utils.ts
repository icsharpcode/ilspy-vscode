'use strict';

import { MsilDecompilerServer } from './server';
import * as protocol from './protocol';

export function decompileAssembly(server: MsilDecompilerServer, request: protocol.DecompileAssemblyRequest) {
    return server.makeRequest<protocol.DecompileResponse>(protocol.Requests.DecompileAssembly, request);
}

export function getTypes(server: MsilDecompilerServer, request: protocol.GetTypesRequest) {
    return server.makeRequest<protocol.GetTypesResponse>(protocol.Requests.GetTypes, request);
}

export function decompileType(server: MsilDecompilerServer, request: protocol.DecompileTypeRequest) {
    return server.makeRequest<protocol.DecompileResponse>(protocol.Requests.DecopmileType, request);
}

export function getMembers(server: MsilDecompilerServer, request: protocol.GetMembersRequest) {
    return server.makeRequest<protocol.GetMembersResponse>(protocol.Requests.GetMembers, request);
}

export function decompileMember(server: MsilDecompilerServer, request: protocol.DecompileMemberRequest) {
    return server.makeRequest<protocol.DecompileResponse>(protocol.Requests.DecompileMember, request);
}
