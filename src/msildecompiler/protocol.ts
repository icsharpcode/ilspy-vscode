/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Modified from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/protocol.ts

'use strict';

export module Requests {
    export const DecompileAssembly = '/assembly';
    export const GetTypes = '/types';
    export const DecopmileType = '/type';
    export const GetMembers = '/members';
    export const DecompileMember = '/member';
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
}

export interface DecompileAssemblyRequest extends Request {
}

export interface DecompileAssemblyResponse {
    Decompiled: string;
}

export interface GetTypesRequest extends Request {
}

export interface MetadataToken {
    RID: number;
    TokenType: number;
}

export interface TypeData {
    Name: string;
    Token: MetadataToken;
}

export interface GetTypesResponse {
    Types: TypeData[];
}

export interface DecompileTypeRequest extends Request {
    Rid: number;
}

export interface DecompileTypeResponse {
    Decompiled: string;
}

export interface GetMembersRequest extends Request {
    Rid: number;
}

export interface GetMembersResponse {
    Types: TypeData[];
}

export interface DecompileMemberRequest extends Request {
    TypeRid: number;
    MemberRid: number;
}

export interface DecompileMemberResponse {
    Decompiled: string;
}
