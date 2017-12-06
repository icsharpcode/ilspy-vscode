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

        const ilspyConfig = vscode.workspace.getConfiguration('ilspy-vscode');

        const path = ilspyConfig.get<string>('path');

        const useMono = ilspyConfig.get<boolean>('useMono');

        const assemblyLoadTimeout = ilspyConfig.get<number>('assemblyLoadTimeout', 60);

        const loggingLevel = ilspyConfig.get<string>('loggingLevel');

        return new Options(path, useMono, loggingLevel, assemblyLoadTimeout);
    }
}