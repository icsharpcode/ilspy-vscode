'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import TelemetryReporter from 'vscode-extension-telemetry';
import * as util from './common';
import { Logger } from './logger';
import { PlatformInformation } from './platform';

let _channel: vscode.OutputChannel = null;

export function activate(context: vscode.ExtensionContext) {

    const extensionId = 'jeremymeng.msil-decompiler';
    const extension = vscode.extensions.getExtension(extensionId);
    const extensionVersion = extension.packageJSON.version;

    util.setExtensionPath(extension.extensionPath);

    _channel = vscode.window.createOutputChannel('C#');

    let logger = new Logger(text => _channel.append(text));

    console.log('Congratulations, your extension "msil-decompiler" is now active!');

    // The command has been defined in the package.json file
    // Now provide the implementation of the command with  registerCommand
    // The commandId parameter must match the command field in package.json
    let disposable = vscode.commands.registerCommand('msildecompiler.decompileAssembly', () => {
        // The code you place here will be executed every time your command is executed

        // Display a message box to the user
        vscode.window.showInformationMessage('Hello World!');
    });

    context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() {
}