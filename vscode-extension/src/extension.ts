/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";

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

  const dotnetCli = "dotnet";
  const backendExecutable = ILSpyBackend.getExecutable(context);
  const serverOptions: ServerOptions = {
    run: { command: dotnetCli, args: [backendExecutable] },
    debug: { command: dotnetCli, args: [backendExecutable] },
  };

  const clientOptions: LanguageClientOptions = {};

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
