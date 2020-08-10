/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as fs from 'fs';
import * as path from 'path';
import * as tempDir from 'temp-dir';
import * as vscode from 'vscode';
import * as util from './common';
import { MsilDecompilerServer } from './msildecompiler/server';
import { DecompiledTreeProvider, MemberNode, LangaugeNames } from './msildecompiler/decompiledTreeProvider';
import { DecompiledCode } from './msildecompiler/protocol';

const tempFileName = new Date().getTime().toString();

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
    disposables.push(vscode.commands.registerCommand('ilspy.decompileAssemblyInWorkspace', async () => {
        // The code you place here will be executed every time your command is executed
        const assembly = await pickAssembly() as AssemblyQuickPickItem;
        await decompileFile(assembly.assemblyPath);
    }));

    disposables.push(vscode.commands.registerCommand('ilspy.decompileAssemblyViaDialog', async () => {
        const file = await promptForAssemblyFilePathViaDialog();
        attemptToDecompileFilePath(file);
    }));

    let lastSelectedNode: MemberNode = null;

    disposables.push(vscode.commands.registerCommand('showDecompiledCode', async (node: MemberNode) => {
        if (lastSelectedNode === node) {
            return;
        }

        lastSelectedNode = node;
        if (node.decompiled) {
            showCode(node.decompiled);
        }
        else {
            const code = await decompileTreeProvider.getCode(node);
            node.decompiled = code;
            showCode(node.decompiled);
        }
    }));

    disposables.push(vscode.commands.registerCommand("ilspy.unloadAssembly", async (node: MemberNode) => {
        if (!node) {
            vscode.window.showInformationMessage('Please use context menu: right-click on the assembly node then select "Unload Assembly"');
            return;
        }
        console.log("Unloading assembly " + node.name);
        const removed = await decompileTreeProvider.removeAssembly(node.name);
        if (removed) {
            decompileTreeProvider.refresh();
        }
    }));

    disposables.push(new vscode.Disposable(() => {
        server.stop();
    }));

    context.subscriptions.push(...disposables);

    function attemptToDecompileFilePath(filePath: string) {
        let escaped: string = filePath.replace(/\\/g, "\\\\");
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

    async function decompileFile(assembly: string) {
        if (!server.isRunning()) {
            await server.restart();
        }
        const added = await decompileTreeProvider.addAssembly(assembly);
        if (added) {
            decompileTreeProvider.refresh();
        }
    }
}

// this method is called when your extension is deactivated
export function deactivate() {
}

function showCode(code: DecompiledCode) {
    showCodeInEditor(code[LangaugeNames.IL], "text", vscode.ViewColumn.Two);
    showCodeInEditor(code[LangaugeNames.CSharp], "csharp", vscode.ViewColumn.One);
}

function showCodeInEditor(code: string, language: string, viewColumn: vscode.ViewColumn) {
    const untitledFileName = `${path.join(tempDir, tempFileName)}.${language === "csharp" ? "cs" : "il"}`;
    const writeStream = fs.createWriteStream(untitledFileName, { flags: "w" });
    writeStream.write(code);
    writeStream.on("finish", async () => {
        try {
            const document = await vscode.workspace.openTextDocument(untitledFileName);
            await vscode.window.showTextDocument(document, viewColumn, true);
            await vscode.commands.executeCommand("revealLine", { lineNumber: 1, at: "top" });
        } catch (errorReason) {
            vscode.window.showErrorMessage("[Error] ilspy-vscode encountered an error while trying to open text document: " + errorReason);
        }
    });
    writeStream.end();
}

async function pickAssembly(): Promise<vscode.QuickPickItem> {
    const assemblies = await findAssemblies();
    const assemblyPathInfo: AssemblyPathInfo[] = parseAssemblyPath(assemblies);
    const quickPickItems = assemblyPathInfo.map(
      info => createAssemblyQuickPickItem(info));
    return await vscode.window.showQuickPick<vscode.QuickPickItem>(quickPickItems);
}

async function findAssemblies(): Promise<string[]> {
    if (!vscode.workspace.rootPath) {
        return Promise.resolve([]);
    }

    const resources = await vscode.workspace.findFiles(
        /*include*/ '{**/*.dll,**/*.exe,**/*.winrt,**/*.netmodule}',
        /*exclude*/ '{**/node_modules/**,**/.git/**,**/bower_components/**}');
    return resources.map(uri => uri.fsPath);
}

async function promptForAssemblyFilePathViaDialog(): Promise<string> {
    const uris = await vscode.window.showOpenDialog(
        /* options*/ {
            openLabel: 'Select assembly',
            canSelectFiles: true,
            canSelectFolders: false,
            canSelectMany: false,
            filters: {
                '.NET Assemblies': ['dll', 'exe', 'winrt', 'netmodule']
            }
        }
    );

    if (uris === undefined) {
        return undefined;
    }

    let strings = uris.map(uri => uri.fsPath);
    if (strings.length > 0) {
        return strings[0];
    } else {
        return undefined;
    }
}

function parseAssemblyPath(assemblies: string[]): AssemblyPathInfo[] {
    const workspaceFolders = vscode.workspace.workspaceFolders;
    return assemblies.map(assemblyPath => {
        const p = path.parse(assemblyPath);
        const assemblyWorkspace = workspaceFolders
          .find(w => p.dir.includes(w.uri.fsPath));
        return {
          fileName: p.base,
          fileExtension: p.ext,
          fullPath: assemblyPath,
          relativePath: p.dir.replace(assemblyWorkspace.uri.fsPath, ''),
          workspaceFolder: assemblyWorkspace.name
        };
    });
}

function createAssemblyQuickPickItem(assemblyPathInfo: AssemblyPathInfo): vscode.QuickPickItem{
    const selectIcon = (extension: string) => {;
      switch (extension) {
        case '.dll':
          return 'library';
        case '.exe':
          return 'file-binary';
        default:
          return 'file';
      }
    }
    const res: AssemblyQuickPickItem = {
        label: `$(${selectIcon(assemblyPathInfo.fileExtension)}) ${assemblyPathInfo.fileName}`,
        description: assemblyPathInfo.fullPath,
        detail: path.join(assemblyPathInfo.workspaceFolder, assemblyPathInfo.relativePath),
        assemblyPath: assemblyPathInfo.fullPath
    };
    return res;
}

interface AssemblyPathInfo {
    fullPath: string;
    relativePath: string;
    fileName: string;
    fileExtension: string;
    workspaceFolder?: string;
}

interface AssemblyQuickPickItem extends vscode.QuickPickItem{
    assemblyPath: string;
}
