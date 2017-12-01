/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Many code snippets are copied from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/

import * as protocol from './protocol';

const priorityCommands = [
];

const normalCommands = [
    protocol.Requests.AddAssembly,
    protocol.Requests.DecompileAssembly,
    protocol.Requests.DecompileMember,
    protocol.Requests.DecopmileType,
    protocol.Requests.ListMembers,
    protocol.Requests.ListTypes,
];

const prioritySet = new Set<string>(priorityCommands);
const normalSet = new Set<string>(normalCommands);
const deferredSet = new Set<string>();

const nonDeferredSet = new Set<string>();

for (let command of priorityCommands) {
    nonDeferredSet.add(command);
}

for (let command of normalCommands) {
    nonDeferredSet.add(command);
}

export function isPriorityCommand(command: string) {
    return prioritySet.has(command);
}

export function isNormalCommand(command: string) {
    return normalSet.has(command);
}

export function isDeferredCommand(command: string) {
    if (deferredSet.has(command)) {
        return true;
    }

    if (nonDeferredSet.has(command)) {
        return false;
    }

    deferredSet.add(command);
    return true;
}