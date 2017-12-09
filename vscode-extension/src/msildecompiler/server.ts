/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Modified from https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/server.ts

import { EventEmitter } from 'events';
import { ChildProcess, exec } from 'child_process';
import { ReadLine, createInterface } from 'readline';
import { launchMsilDecompiler } from './launcher';
import { Options } from './options';
import { Logger } from '../logger';
import { Request, RequestQueueCollection } from './requestQueue';
import * as os from 'os';
import * as protocol from './protocol';
import * as vscode from 'vscode';

enum ServerState {
    Starting,
    Started,
    Stopped
}

module Events {
    export const StateChanged = 'stateChanged';

    export const StdOut = 'stdout';
    export const StdErr = 'stderr';

    export const Error = 'Error';
    export const ServerError = 'ServerError';

    export const BeforeServerInstall = 'BeforeServerInstall';
    export const BeforeServerStart = 'BeforeServerStart';
    export const ServerStart = 'ServerStart';
    export const ServerStop = 'ServerStop';

    export const Started = 'started';
}

export class MsilDecompilerServer {

    private static _nextId = 1;

    private _debugMode: boolean = true;

    private _readLine: ReadLine;
    private _disposables: vscode.Disposable[] = [];

    private _eventBus = new EventEmitter();
    private _state: ServerState = ServerState.Stopped;
    private _channel: vscode.OutputChannel;
    private _requestQueue: RequestQueueCollection;
    private _logger: Logger;

    private _isDebugEnable: boolean = false;

    private _serverProcess: ChildProcess;
    private _options: Options;

    private _assemblyPaths: Set<string> = new Set<string>();

    constructor() {
        this._channel = vscode.window.createOutputChannel('ilspy-vscode');
        this._logger = new Logger(message => this._channel.append(message));

        const logger = this._debugMode
            ? this._logger
            : new Logger(message => { });

        this._requestQueue = new RequestQueueCollection(logger, 8, request => this._makeRequest(request));
    }

    public get assemblyPaths() {
        return this._assemblyPaths;
    }

    public isRunning(): boolean {
        return this._state === ServerState.Started;
    }

    private _getState(): ServerState {
        return this._state;
    }

    private _setState(value: ServerState): void {
        if (typeof value !== 'undefined' && value !== this._state) {
            this._state = value;
            this._fireEvent(Events.StateChanged, this._state);
        }
    }

    public getChannel(): vscode.OutputChannel {
        return this._channel;
    }

    public isDebugEnable(): boolean {
        return this._isDebugEnable;
    }

    // --- eventing

    public onStdout(listener: (e: string) => any, thisArg?: any) {
        return this._addListener(Events.StdOut, listener, thisArg);
    }

    public onStderr(listener: (e: string) => any, thisArg?: any) {
        return this._addListener(Events.StdErr, listener, thisArg);
    }

    public onServerError(listener: (err: any) => any, thisArg?: any) {
        return this._addListener(Events.ServerError, listener, thisArg);
    }

    public onBeforeServerInstall(listener: () => any) {
        return this._addListener(Events.BeforeServerInstall, listener);
    }

    public onBeforeServerStart(listener: (e: string) => any) {
        return this._addListener(Events.BeforeServerStart, listener);
    }

    public onServerStart(listener: (e: string) => any) {
        return this._addListener(Events.ServerStart, listener);
    }

    public onServerStop(listener: () => any) {
        return this._addListener(Events.ServerStop, listener);
    }

    public onMsilDecompilerStart(listener: () => any) {
        return this._addListener(Events.Started, listener);
    }

    private _addListener(event: string, listener: (e: any) => any, thisArg?: any): vscode.Disposable {
        listener = thisArg ? listener.bind(thisArg) : listener;
        this._eventBus.addListener(event, listener);
        return new vscode.Disposable(() => this._eventBus.removeListener(event, listener));
    }

    protected _fireEvent(event: string, args: any): void {
        this._eventBus.emit(event, args);
    }

    // --- start, stop, and connect

    private _start(): Promise<void> {
        this._setState(ServerState.Starting);
        this._assemblyPaths.clear();

        let args = [
            '--stdio',
            '--encoding', 'utf-8',
            //TODO: '--loglevel', this._options.loggingLevel
        ];

        this._options = Options.Read();

        this._logger.appendLine(`Starting ILSpy.Host server at ${new Date().toLocaleString()}`);
        this._logger.increaseIndent();
        this._logger.decreaseIndent();
        this._logger.appendLine();

        this._fireEvent(Events.BeforeServerStart, 0);

        const cwd = "";
        return launchMsilDecompiler(cwd, args).then(value => {
            if (value.usingMono) {
                this._logger.appendLine(`ILSpy.Host server started wth Mono`);
            }
            else {
                this._logger.appendLine(`ILSpy.Host server started`);
            }

            this._logger.increaseIndent();
            this._logger.appendLine(`Path: ${value.command}`);
            this._logger.appendLine(`PID: ${value.process.pid}`);
            this._logger.decreaseIndent();
            this._logger.appendLine();

            this._serverProcess = value.process;
            this._setState(ServerState.Started);
            this._fireEvent(Events.ServerStart, 0);

            return this._doConnect();
        }).then(() => {
            this._requestQueue.drain();
        }).catch(err => {
            this._fireEvent(Events.ServerError, err);
            return this.stop();
        });
    }

    public stop(): Promise<void> {

        while (this._disposables.length) {
            this._disposables.pop().dispose();
        }

        let cleanupPromise: Promise<void>;

        if (!this._serverProcess) {
            // nothing to kill
            cleanupPromise = Promise.resolve();
        }
        else if (process.platform === 'win32') {
            // when killing a process in windows its child
            // processes are *not* killed but become root
            // processes. Therefore we use TASKKILL.EXE
            cleanupPromise = new Promise<void>((resolve, reject) => {
                const killer = exec(`taskkill /F /T /PID ${this._serverProcess.pid}`, (err, stdout, stderr) => {
                    if (err) {
                        return reject(err);
                    }
                });

                killer.on('exit', resolve);
                killer.on('error', reject);
            });
        }
        else {
            // Kill Unix process
            this._serverProcess.kill('SIGTERM');
            cleanupPromise = Promise.resolve();
        }

        return cleanupPromise.then(() => {
            this._serverProcess = null;
            this._setState(ServerState.Stopped);
            this._fireEvent(Events.ServerStop, this);
        });
    }

    public restart(): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            this._assemblyPaths.clear();
            return this.stop().then(() => {
                this._start().then(
                    () => {
                        resolve();
                    },
                    (reason) => {
                        reject();
                    });
            });
        });
    }

    // --- requests et al

    public makeRequest<TResponse>(command: string, data?: any, token?: vscode.CancellationToken): Promise<TResponse> {

        if (this._getState() !== ServerState.Started) {
            return Promise.reject<TResponse>('server has been stopped or not started');
        }

        let request: Request;

        let promise = new Promise<TResponse>((resolve, reject) => {
            request = {
                command,
                data,
                onSuccess: value => resolve(value),
                onError: err => reject(err)
            };

            this._requestQueue.enqueue(request);
        });

        if (token) {
            token.onCancellationRequested(() => {
                this._requestQueue.cancelRequest(request);
            });
        }

        return promise.then(response => {
            return response;
        });
    }

    private _doConnect(): Promise<void> {

        this._serverProcess.stderr.on('data', (data: any) => {
            this._fireEvent('stderr', String(data));
        });


        this._readLine = createInterface({
            input: this._serverProcess.stdout,
            output: this._serverProcess.stdin,
            terminal: false
        });

        const promise = new Promise<void>((resolve, reject) => {
            let listener: vscode.Disposable;

            // Convert the timeout from the seconds to milliseconds, which is required by setTimeout().
            const timeoutDuration = this._options.assemblyLoadTimeout * 1000;

            // timeout logic
            const handle = setTimeout(() => {
                if (listener) {
                    listener.dispose();
                }

                reject(new Error("ILSpy.Host server load timed out. Use the 'ilspy-vscode.assemblyLoadTimeout' setting to override the default delay (one minute)."));
            }, timeoutDuration);

            // handle started-event
            listener = this.onMsilDecompilerStart(() => {
                if (listener) {
                    listener.dispose();
                }

                clearTimeout(handle);
                resolve();
            });
        });

        const lineReceived = this._onLineReceived.bind(this);

        this._readLine.addListener('line', lineReceived);

        this._disposables.push(new vscode.Disposable(() => {
            this._readLine.removeListener('line', lineReceived);
        }));

        return promise;
    }

    private _onLineReceived(line: string) {
        if (line[0] !== '{') {
            this._logger.appendLine(line);
            return;
        }

        let packet: protocol.WireProtocol.Packet;
        try {
            packet = JSON.parse(line);
        }
        catch (err) {
            // This isn't JSON
            return;
        }

        if (!packet.Type) {
            // Bogus packet
            return;
        }

        switch (packet.Type) {
            case 'response':
                this._handleResponsePacket(<protocol.WireProtocol.ResponsePacket>packet);
                break;
            case 'event':
                this._handleEventPacket(<protocol.WireProtocol.EventPacket>packet);
                break;
            default:
                console.warn(`Unknown packet type: ${packet.Type}`);
                break;
        }
    }

    private _handleResponsePacket(packet: protocol.WireProtocol.ResponsePacket) {
        const request = this._requestQueue.dequeue(packet.Command, packet.Request_seq);

        if (!request) {
            this._logger.appendLine(`Received response for ${packet.Command} but could not find request.`);
            return;
        }

        if (this._debugMode) {
            this._logger.appendLine(`handleResponse: ${packet.Command} (${packet.Request_seq})`);
        }

        if (packet.Success) {
            request.onSuccess(packet.Body);
        }
        else {
            request.onError(packet.Message || packet.Body);
        }

        this._requestQueue.drain();
    }

    private _handleEventPacket(packet: protocol.WireProtocol.EventPacket): void {
        if (packet.Event === 'log') {
            const entry = <{ LogLevel: string; Name: string; Message: string; }>packet.Body;
            this._logOutput(entry.LogLevel, entry.Name, entry.Message);
        }
        else {
            // fwd all other events
            this._fireEvent(packet.Event, packet.Body);
        }
    }

    private _makeRequest(request: Request) {
        const id = MsilDecompilerServer._nextId++;

        const requestPacket: protocol.WireProtocol.RequestPacket = {
            Type: 'request',
            Seq: id,
            Command: request.command,
            Arguments: request.data
        };

        if (this._debugMode) {
            this._logger.append(`makeRequest: ${request.command} (${id})`);
            if (request.data) {
                this._logger.append(`, data=${JSON.stringify(request.data)}`);
            }
            this._logger.appendLine();
        }

        this._serverProcess.stdin.write(JSON.stringify(requestPacket) + '\n');

        return id;
    }

    private static getLogLevelPrefix(logLevel: string) {
        switch (logLevel) {
            case "TRACE": return "trce";
            case "DEBUG": return "dbug";
            case "INFORMATION": return "info";
            case "WARNING": return "warn";
            case "ERROR": return "fail";
            case "CRITICAL": return "crit";
            default: throw new Error(`Unknown log level value: ${logLevel}`);
        }
    }

    private _isFilterableOutput(logLevel: string, name: string, message: string) {
        // filter messages like: /codecheck: 200 339ms
        const timing200Pattern = /^\/[\/\w]+: 200 \d+ms/;

        return logLevel === "INFORMATION"
            && name === "MsilDecompilerServer.Middleware.LoggingMiddleware"
            && timing200Pattern.test(message);
    }

    private _logOutput(logLevel: string, name: string, message: string) {
        if (this._debugMode || !this._isFilterableOutput(logLevel, name, message)) {
            let output = `[${MsilDecompilerServer.getLogLevelPrefix(logLevel)}]: ${name}${os.EOL}${message}`;

            const newLinePlusPadding = os.EOL + "        ";
            output = output.replace(os.EOL, newLinePlusPadding);

            this._logger.appendLine(output);
        }
    }
}
