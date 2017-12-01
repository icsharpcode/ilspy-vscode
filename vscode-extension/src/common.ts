/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Copied from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/common.ts

import * as cp from 'child_process';
import * as fs from 'fs';
import * as path from 'path';

let extensionPath: string;

export function setExtensionPath(path: string) {
    extensionPath = path;
}

export function getExtensionPath() {
    if (!extensionPath) {
        throw new Error('Failed to set extension path');
    }

    return extensionPath;
}

export function getBinPath() {
    return path.resolve(getExtensionPath(), "bin");
}

export function isBoolean(obj: any): obj is boolean {
    return obj === true || obj === false;
}

export function buildPromiseChain<T, TResult>(array: T[], builder: (item: T) => Promise<TResult>): Promise<TResult> {
    return array.reduce(
        (promise, n) => promise.then(() => builder(n)),
        Promise.resolve<TResult>(null));
}

export function execChildProcess(command: string, workingDirectory: string = getExtensionPath()): Promise<string> {
    return new Promise<string>((resolve, reject) => {
        cp.exec(command, { cwd: workingDirectory, maxBuffer: 500 * 1024 }, (error, stdout, stderr) => {
            if (error) {
                reject(error);
            }
            else if (stderr && stderr.length > 0) {
                reject(new Error(stderr));
            }
            else {
                resolve(stdout);
            }
        });
    });
}

export function fileExists(filePath: string): Promise<boolean> {
    return new Promise<boolean>((resolve, reject) => {
        fs.stat(filePath, (err, stats) => {
            if (stats && stats.isFile()) {
                resolve(true);
            }
            else {
                resolve(false);
            }
        });
    });
}

export enum InstallFileType {
    Begin,
    Lock
}

function getInstallFilePath(type: InstallFileType): string {
    let installFile = 'install.' + InstallFileType[type];
    return path.resolve(getExtensionPath(), installFile);
}

export function installFileExists(type: InstallFileType): Promise<boolean> {
    return fileExists(getInstallFilePath(type));
}

export function touchInstallFile(type: InstallFileType): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        fs.writeFile(getInstallFilePath(type), '', err => {
            if (err) {
                reject(err);
                return;
            }

            resolve();
        });
    });
}

export function deleteInstallFile(type: InstallFileType): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        fs.unlink(getInstallFilePath(type), err => {
            if (err) {
                reject(err);
                return;
            }

            resolve();
        });
    });
}