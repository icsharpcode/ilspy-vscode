/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as path from "path";

import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions,
  Trace,
} from "vscode-languageclient/node";
import ILSpyBackend from "./decompiler/ILSpyBackend";
import { DecompiledTreeProvider } from "./decompiler/DecompiledTreeProvider";
import { registerDecompileAssemblyInWorkspace } from "./commands/decompileAssemblyInWorkspace";
import { registerDecompileAssemblyViaDialog } from "./commands/decompileAssemblyViaDialog";
import { registerShowDecompiledCode } from "./commands/showDecompiledCode";
import { registerUnloadAssembly } from "./commands/unloadAssembly";

let client: LanguageClient;

export function activate(context: vscode.ExtensionContext) {
  const disposables: vscode.Disposable[] = [];

  let backendExecutable = ILSpyBackend.getExecutable(context);
  let serverOptions: ServerOptions = {
    run: { command: backendExecutable },
    debug: { command: backendExecutable },
  };

  let clientOptions: LanguageClientOptions = {};

  client = new LanguageClient(
    "ilspy-backend",
    "ILSpy Backend",
    serverOptions,
    clientOptions
  );
  client.trace = Trace.Verbose;

  const ilspyBackend = new ILSpyBackend(client);

  const decompileTreeProvider = new DecompiledTreeProvider(ilspyBackend);
  disposables.push(
    vscode.window.registerTreeDataProvider(
      "ilspyDecompiledMembers",
      decompileTreeProvider
    )
  );

  disposables.push(registerDecompileAssemblyInWorkspace(decompileTreeProvider));
  disposables.push(registerDecompileAssemblyViaDialog(decompileTreeProvider));
  disposables.push(registerShowDecompiledCode(decompileTreeProvider));
  disposables.push(registerUnloadAssembly(decompileTreeProvider));

  client.start();

  context.subscriptions.push(...disposables);
}

export function deactivate(): Thenable<void> | undefined {
  if (!client) {
    return undefined;
  }
  return client.stop();
}
