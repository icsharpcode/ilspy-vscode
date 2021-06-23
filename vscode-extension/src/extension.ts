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
import {
  DecompiledTreeProvider,
  MemberNode,
} from "./decompiler/DecompiledTreeProvider";
import { registerDecompileAssemblyInWorkspace } from "./commands/decompileAssemblyInWorkspace";
import { registerDecompileAssemblyViaDialog } from "./commands/decompileAssemblyViaDialog";
import { registerShowDecompiledCode } from "./commands/showDecompiledCode";
import { registerUnloadAssembly } from "./commands/unloadAssembly";
import { acquireDotnetRuntime } from "./dotnet-acquire/acquire";
import OutputWindowLogger from "./OutputWindowLogger";
import { stat } from "fs";

let client: LanguageClient;

export async function activate(context: vscode.ExtensionContext) {
  const disposables: vscode.Disposable[] = [];

  const logger = new OutputWindowLogger();

  setBackendAvailable(false);

  const dotnetCli = await acquireDotnetRuntime(context, logger);
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
      switch (e.newState) {
        case State.Running:
          logger.writeLine("ILSpy Backend is running");
          setBackendAvailable(true);
          break;
        case State.Starting:
          logger.writeLine("ILSpy Backend is starting...");
          setBackendAvailable(false);
          break;
        case State.Stopped:
          logger.writeLine("ILSpy Backend has stopped");
          setBackendAvailable(false);
          break;
      }
    });

    logger.writeLine(`Launch ILSpy Backend: ${backendExecutable}`);
    client.start();
  }

  const ilspyBackend = new ILSpyBackend(client);
  const decompileTreeProvider = new DecompiledTreeProvider(ilspyBackend);
  const decompileTreeView: vscode.TreeView<MemberNode> =
    vscode.window.createTreeView("ilspyDecompiledMembers", {
      treeDataProvider: decompileTreeProvider,
    });
  disposables.push(decompileTreeView);

  disposables.push(
    registerDecompileAssemblyInWorkspace(
      decompileTreeProvider,
      decompileTreeView
    )
  );
  disposables.push(
    registerDecompileAssemblyViaDialog(decompileTreeProvider, decompileTreeView)
  );
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
