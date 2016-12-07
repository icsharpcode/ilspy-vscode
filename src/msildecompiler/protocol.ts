/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Modified from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/protocol.ts

'use strict';

export module Requests {
    export const DecompileAssembly = '/assembly';
    export const GetTypes = '/types';
    export const DecopmileType = '/types/{rid}';
    export const GetMembers = '/types/{rid}/members';
    export const DecompileMember = '/types/{typeRid}/members/{memberRid}';
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
}

export interface Request {
    Filename: string;
    Line?: number;
    Column?: number;
    Buffer?: string;
}

export interface AssemblyArgument {
}

export interface TypesArgument {
}

export interface TypeArgument {
    TypeRuntimeId: number;
}

export interface MemberArgument {
    TypeRuntimeId: number;
    MemberRuntimeId: number;
}