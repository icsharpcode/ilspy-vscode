/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

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
import { registerUnloadAssembly } from "./commands/unloadAssembly";
import { acquireDotnetRuntime } from "./dotnet-acquire/acquire";
import OutputWindowLogger from "./OutputWindowLogger";
import {
  commands,
  Disposable,
  ExtensionContext,
  TreeView,
  window,
  workspace,
} from "vscode";
import { DecompilerTextDocumentContentProvider } from "./decompiler/DecompilerTextDocumentContentProvider";
import { MemberNode } from "./decompiler/MemberNode";
import { registerShowCode } from "./commands/showCode";
import { registerSelectOutputLanguage } from "./commands/selectOutputLanguage";
import { ILSPY_URI_SCHEME } from "./decompiler/memberNodeUri";
import { registerSearch } from "./commands/search";

let client: LanguageClient;

export async function activate(context: ExtensionContext) {
  const disposables: Disposable[] = [];

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

    logger.writeLine(`ILSpy Backend: ${backendExecutable}`);
    logger.writeLine(`Starting LSP client...`);
    client.start();
    logger.writeLine(`Started LSP client`);
  }

  const ilspyBackend = new ILSpyBackend(client);
  const decompileTreeProvider = new DecompiledTreeProvider(ilspyBackend);
  const decompileTreeView: TreeView<MemberNode> = window.createTreeView(
    "ilspyDecompiledMembers",
    {
      treeDataProvider: decompileTreeProvider,
    }
  );
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

  disposables.push(registerSearch(ilspyBackend));

  const decompilerTextDocumentContentProvider =
    new DecompilerTextDocumentContentProvider(ilspyBackend);

  disposables.push(
    workspace.registerTextDocumentContentProvider(
      ILSPY_URI_SCHEME,
      decompilerTextDocumentContentProvider
    )
  );

  disposables.push(registerShowCode(decompilerTextDocumentContentProvider));

  disposables.push(
    registerSelectOutputLanguage(decompilerTextDocumentContentProvider)
  );
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
  commands.executeCommand("setContext", "ilspy.backendAvailable", available);
}
