/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Modified from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/options.ts

import * as vscode from 'vscode';

export class Options {
    constructor(
        public path?: string,
        public useMono?: boolean,
        public loggingLevel?: string,
        public assemblyLoadTimeout?: number) { }

    public static Read(): Options {

        const msildecompilerConfig = vscode.workspace.getConfiguration('msildecompiler');

        const path = msildecompilerConfig.get<string>('path');

        const useMono = msildecompilerConfig.get<boolean>('useMono');

        const assemblyLoadTimeout = msildecompilerConfig.get<number>('projectLoadTimeout', 60);

        const loggingLevel = msildecompilerConfig.get<string>('loggingLevel');

        return new Options(path, useMono, loggingLevel, assemblyLoadTimeout);
    }
}