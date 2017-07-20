// Many code snippets are copied from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/launcher.ts

'use strict';

import { spawn, ChildProcess } from 'child_process';
import { satisfies } from 'semver';
import { PlatformInformation } from '../platform';
import * as path from 'path';
import * as vscode from 'vscode';
import * as util from '../common';
import { Options } from './options';

export interface LaunchResult {
    process: ChildProcess;
    command: string;
    usingMono: boolean;
}

export function launchMsilDecompiler(cwd: string, args: string[]): Promise<LaunchResult> {
    return new Promise<LaunchResult>((resolve, reject) => {
        launch(cwd, args)
            .then(result => {
                // async error - when target not not ENEOT
                result.process.on('error', err => {
                    reject(err);
                });

                // success after a short freeing event loop
                setTimeout(function () {
                    resolve(result);
                }, 0);
            });
    });
}

function launch(cwd: string, args: string[]): Promise<LaunchResult> {
    return PlatformInformation.GetCurrent().then(platformInfo => {
        const options = Options.Read();

        if (options.path && options.useMono) {
            return launchNixMono(options.path, cwd, args);
        }

        const launchPath = options.path || getLaunchPath(platformInfo);

        if (platformInfo.isWindows()) {
            return launchWindows(launchPath, cwd, args);
        }
        else {
            return launchNix(launchPath, cwd, args);
        }
    });
}

function getLaunchPath(platformInfo: PlatformInformation): string {
    const binPath = util.getBinPath();

    return platformInfo.isWindows()
        ? path.join(binPath, 'msildecompiler', 'MsilDecompiler.Host.exe')
        : path.join(binPath, 'run');
}

function launchWindows(launchPath: string, cwd: string, args: string[]): LaunchResult {
    function escapeIfNeeded(arg: string) {
        const hasSpaceWithoutQuotes = /^[^"].* .*[^"]/;
        return hasSpaceWithoutQuotes.test(arg)
            ? `"${arg}"`
            : arg.replace("&","^&");
    }

    let argsCopy = args.slice(0); // create copy of args
    argsCopy.unshift(launchPath);
    argsCopy = [[
        '/s',
        '/c',
        '"' + argsCopy.map(escapeIfNeeded).join(' ') + '"'
    ].join(' ')];

    let process = spawn('cmd', argsCopy, <any>{
        windowsVerbatimArguments: true,
        detached: false,
        cwd: cwd
    });

    return {
        process,
        command: launchPath,
        usingMono: false
    };
}

function launchNix(launchPath: string, cwd: string, args: string[]): LaunchResult {
    let process = spawn(launchPath, args, {
        detached: false,
        cwd: cwd
    });

    return {
        process,
        command: launchPath,
        usingMono: true
    };
}

function launchNixMono(launchPath: string, cwd: string, args: string[]): Promise<LaunchResult> {
    return canLaunchMono()
        .then(() => {
            let argsCopy = args.slice(0); // create copy of details args
            argsCopy.unshift(launchPath);

            let process = spawn('mono', argsCopy, {
                detached: false,
                cwd: cwd
            });

            return {
                process,
                command: launchPath,
                usingMono: true
            };
        });
}

function canLaunchMono(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        hasMono('>=4.6.0').then(success => {
            if (success) {
                resolve();
            }
            else {
                reject(new Error('Cannot start MsilDecompiler because Mono version >=4.6.0 is required.'));
            }
        });
    });
}

export function hasMono(range?: string): Promise<boolean> {
    const versionRegexp = /(\d+\.\d+\.\d+)/;

    return new Promise<boolean>((resolve, reject) => {
        let childprocess: ChildProcess;
        try {
            childprocess = spawn('mono', ['--version']);
        }
        catch (e) {
            return resolve(false);
        }

        childprocess.on('error', function (err: any) {
            resolve(false);
        });

        let stdout = '';
        childprocess.stdout.on('data', (data: NodeBuffer) => {
            stdout += data.toString();
        });

        childprocess.stdout.on('close', () => {
            let match = versionRegexp.exec(stdout),
                ret: boolean;

            if (!match) {
                ret = false;
            }
            else if (!range) {
                ret = true;
            }
            else {
                ret = satisfies(match[1], range);
            }

            resolve(ret);
        });
    });
}