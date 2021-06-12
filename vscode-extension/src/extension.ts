/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";

import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions,
  State,
  Trace,
} from "vscode-languageclient/node";
import ILSpyBackend from "./decompiler/ILSpyBackend";
import { DecompiledTreeProvider } from "./decompiler/DecompiledTreeProvider";
import { registerDecompileAssemblyInWorkspace } from "./commands/decompileAssemblyInWorkspace";
import { registerDecompileAssemblyViaDialog } from "./commands/decompileAssemblyViaDialog";
import { registerShowDecompiledCode } from "./commands/showDecompiledCode";
import { registerUnloadAssembly } from "./commands/unloadAssembly";
import { acquireDotnetRuntime } from "./dotnet-acquire/acquire";

let client: LanguageClient;

export async function activate(context: vscode.ExtensionContext) {
  const disposables: vscode.Disposable[] = [];

  setBackendAvailable(false);

  const dotnetCli = await acquireDotnetRuntime(context);
  if (dotnetCli) {
    const backendExecutable = ILSpyBackend.getExecutable(context);
    const serverOptions: ServerOptions = {
      run: { command: dotnetCli, args: [backendExecutable], options: {} },
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

    client.onDidChangeState((e) => {
      if (e.newState === State.Running) {
        setBackendAvailable(true);
      } else {
        setBackendAvailable(false);
      }
    });

    client.start();
  }

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

  context.subscriptions.push(...disposables);
}

export function deactivate(): Thenable<void> | undefined {
  if (!client) {
    return undefined;
  }
  return client.stop();
}

function setBackendAvailable(available: boolean) {
  vscode.commands.executeCommand(
    "setContext",
    "ilspy.backendAvailable",
    available
  );
}
