/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

 'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as fs from 'fs';
import * as vscode from 'vscode';
import * as util from './common';
import { MsilDecompilerServer } from './msildecompiler/server';
import { DecompiledTreeProvider, MemberNode, LangaugeNames } from './msildecompiler/decompiledTreeProvider';
import { DecompiledCode } from './msildecompiler/protocol';

let csharpEditor: vscode.TextEditor = null;
let ilEditor: vscode.TextEditor = null;


export function activate(context: vscode.ExtensionContext) {

    const extensionId = 'icsharpcode.ilspy-vscode';
    const extension = vscode.extensions.getExtension(extensionId);

    util.setExtensionPath(extension.extensionPath);

    const server = new MsilDecompilerServer();
    let decompileTreeProvider = new DecompiledTreeProvider(server);
    const disposables: vscode.Disposable[] = [];

    console.log('Congratulations, your extension "ilspy-vscode" is now active!');

    decompileTreeProvider = new DecompiledTreeProvider(server);
    disposables.push(vscode.window.registerTreeDataProvider("ilspyDecompiledMembers", decompileTreeProvider));

    // The command has been defined in the package.json file
    // Now provide the implementation of the command with  registerCommand
    // The commandId parameter must match the command field in package.json
    disposables.push(vscode.commands.registerCommand('ilspy.decompileAssemblyInWorkspace', () => {
        // The code you place here will be executed every time your command is executed
        pickAssembly().then(assembly => {
            decompileFile(assembly);
        });
    }));

    disposables.push(vscode.commands.registerCommand('ilspy.decompileAssemblyViaDialog', () => {
        promptForAssemblyFilePathViaDialog().then(attemptToDecompileFilePath);
    }));

    let lastSelectedNode: MemberNode = null;

    disposables.push(vscode.commands.registerCommand('showDecompiledCode', (node: MemberNode) => {
        if (lastSelectedNode === node) {
            return;
        }

        lastSelectedNode = node;
        if (node.decompiled) {
            showCode(node.decompiled);
        }
        else {
            decompileTreeProvider.getCode(node).then(code => {
                node.decompiled = code;
                showCode(node.decompiled);
            });
        }
    }));

    disposables.push(vscode.commands.registerCommand("ilspy.unloadAssembly", (node: MemberNode) => {
        console.log("Unloading assembly " + node.name);
        decompileTreeProvider.removeAssembly(node.name).then(removed => {
            if (removed) {
                decompileTreeProvider.refresh();
            }
        });
    }));

    disposables.push(new vscode.Disposable(() => {
        server.stop();
    }));

    context.subscriptions.push(...disposables);

    function attemptToDecompileFilePath(filePath: string) {
        let escaped: string = filePath.replace(/\\/g, "\\\\",);
        if (escaped[0] === '"' && escaped[escaped.length - 1] === '"') {
            escaped = escaped.slice(1, -1);
        }

        try {
            fs.accessSync(escaped, fs.constants.R_OK);
            decompileFile(escaped);
        } catch (err) {
            vscode.window.showErrorMessage('cannot read the file ' + filePath);
        }
    }

    function decompileFile(assembly: string) {
        if(!server.isRunning()) {
            server.restart().then(() => {
                decompileTreeProvider.addAssembly(assembly).then(added => {
                    if(added) {
                        decompileTreeProvider.refresh();
                }});
            });
        }
        else {
            decompileTreeProvider.addAssembly(assembly).then(res => {
                if(res) {
                    decompileTreeProvider.refresh();
                }
            });
        }
    }
}

// this method is called when your extension is deactivated
export function deactivate() {
}

function showCode(code: DecompiledCode) {
    showCodeInEditor(csharpEditor, code[LangaugeNames.CSharp], "csharp", vscode.ViewColumn.One);
    showCodeInEditor(ilEditor, code[LangaugeNames.IL], "text", vscode.ViewColumn.Two);
}

function showCodeInEditor(editor: vscode.TextEditor, code: string, language: string, viewColumn: vscode.ViewColumn) {
    if (!editor) {
        vscode.workspace.openTextDocument(
            {
                "content": code,
                "language": language
            },
        ).then(document => {
            vscode.window.showTextDocument(document, viewColumn).then(ed => editor = ed);
        }, errorReason => {
           console.log("[Error] ilspy-vscode encountered en error while trying to show code" + errorReason);
        });
    }
    else {
        replaceCode(editor, code);
    }
}

function replaceCode(editor: vscode.TextEditor, code: string) {
    const firstLine = editor.document.lineAt(0);
    const lastLine = editor.document.lineAt(editor.document.lineCount - 1);
    const range = new vscode.Range(0, firstLine.range.start.character, editor.document.lineCount - 1, lastLine.range.end.character);
    editor.edit(editBuilder => editBuilder.replace(range, code));
    vscode.commands.executeCommand("cursorMove", { "to": "viewPortTop" });
}

function pickAssembly(): Thenable<string> {
    return findAssemblies().then(assemblies => {
        return vscode.window.showQuickPick(assemblies);
    });
}

function findAssemblies(): Thenable<string[]> {
    if (!vscode.workspace.rootPath) {
        return Promise.resolve([]);
    }

    return vscode.workspace.findFiles(
        /*include*/ '{**/*.dll,**/*.exe,**/*.winrt,**/*.netmodule}',
        /*exclude*/ '{**/node_modules/**,**/.git/**,**/bower_components/**}')
    .then(resources => {
        return resources.map(uri => uri.fsPath);
    });
}

function promptForAssemblyFilePathViaDialog(): Thenable<string> {
    return vscode.window.showOpenDialog(
        /* options*/ {
            openLabel: 'Select assembly',
            canSelectFiles: true,
            canSelectFolders: false,
            canSelectMany: false,
        }
    )
    .then(uris => {
        if (uris === undefined) {
            return undefined;
        }

        let strings = uris.map(uri => uri.fsPath);
        if (strings.length > 0) {
            return strings[0];
        } else {
            return undefined;
        }
    });
}
