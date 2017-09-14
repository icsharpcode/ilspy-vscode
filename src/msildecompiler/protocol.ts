/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Modified from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/protocol.ts

'use strict';

export module Requests {
    export const AddAssembly = '/addassembly';
    export const DecompileAssembly = '/decompileassembly';
    export const ListTypes = '/listtypes';
    export const DecopmileType = '/decompiletype';
    export const ListMembers = '/listmembers';
    export const DecompileMember = '/decompilemember';
}

export namespace WireProtocol {
    export interface Packet {
        Type: string;
        Seq: number;
    }

    export interface RequestPacket extends Packet {
        Command: string;
        Arguments: any;
    }

    export interface ResponsePacket extends Packet {
        Command: string;
        Request_seq: number;
        Running: boolean;
        Success: boolean;
        Message: string;
        Body: any;
    }

    export interface EventPacket extends Packet {
        Event: string;
        Body: any;
    }
}


export interface Request {
    AssemblyPath: string;
}

export interface AddAssemblyRequest extends Request {
}

export interface AddAssemblyResponse {
    Added: boolean;
}

export interface DecompileAssemblyRequest extends Request {
}

export interface DecompileResponse {
    Decompiled: string;
}

export interface ListTypesRequest extends Request {
}

export interface MetadataToken {
    RID: number;
    TokenType: number;
}

export interface MemberData {
    Name: string;
    Token: MetadataToken;
}

export interface ListTypesResponse {
    Types: MemberData[];
}

export interface DecompileTypeRequest extends Request {
    Rid: number;
}

export interface ListMembersRequest extends Request {
    Rid: number;
}

export interface ListMembersResponse {
    Members: MemberData[];
}

export interface DecompileMemberRequest extends Request {
    TypeRid: number;
    MemberType: number;
    MemberRid: number;
}