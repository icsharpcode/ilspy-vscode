/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

 // Mimicing https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/utils.ts

'use strict';

import { MsilDecompilerServer } from './server';
import * as protocol from './protocol';

export function addAssembly(server: MsilDecompilerServer, request: protocol.AddAssemblyRequest) {
    return server.makeRequest<protocol.AddAssemblyResponse>(protocol.Requests.AddAssembly, request);
}

export function decompileAssembly(server: MsilDecompilerServer, request: protocol.DecompileAssemblyRequest) {
    return server.makeRequest<protocol.DecompileResponse>(protocol.Requests.DecompileAssembly, request);
}

export function listNamespaces(server: MsilDecompilerServer, request: protocol.ListNamespacesRequest) {
    return server.makeRequest<protocol.ListNamespacesResponse>(protocol.Requests.ListNamespaces, request);
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
