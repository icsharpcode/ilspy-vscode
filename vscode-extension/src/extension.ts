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
import { registerDecompileSelectedAssembly } from "./commands/decompileSelectedAssembly";
import { registerReloadAssembly } from "./commands/reloadAssembly";
import { registerUnloadAssembly } from "./commands/unloadAssembly";
import { acquireDotnetRuntime } from "./dotnet-acquire/acquire";
import OutputWindowLogger from "./OutputWindowLogger";
import { commands, Disposable, ExtensionContext, workspace } from "vscode";
import { DecompilerTextDocumentContentProvider } from "./decompiler/DecompilerTextDocumentContentProvider";
import {
  registerSelectOutputLanguage,
  registerSelectOutputLanguageStatusBarItem,
} from "./commands/selectOutputLanguage";
import { ILSPY_URI_SCHEME } from "./decompiler/nodeUri";
import { registerSearch } from "./commands/search";
import { SearchResultTreeProvider } from "./decompiler/search/SearchResultTreeProvider";
import { registerDecompileNode } from "./commands/decompileNode";
import {
  createDecompiledTreeView,
  createSearchResultTreeView,
} from "./view/treeViews";
import { registerSearchEditorSelection } from "./commands/searchEditorSelection";

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
    await client.setTrace(Trace.Verbose);

    client.onDidChangeState((e) => {
      switch (e.newState) {
        case State.Running:
          logger.writeLine("ILSpy LSP Backend is running");
          setBackendAvailable(true);
          break;
        case State.Starting:
          logger.writeLine("ILSpy LSP Backend is starting...");
          setBackendAvailable(false);
          break;
        case State.Stopped:
          logger.writeLine("ILSpyF LSP Backend has stopped");
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
  const decompiledTreeProvider = new DecompiledTreeProvider(
    context,
    ilspyBackend
  );
  const decompiledTreeView = createDecompiledTreeView(decompiledTreeProvider);
  disposables.push(decompiledTreeView);
  decompiledTreeProvider.initWithAssemblies();

  disposables.push(
    registerDecompileAssemblyInWorkspace(
      decompiledTreeProvider,
      decompiledTreeView
    )
  );
  disposables.push(
    registerDecompileAssemblyViaDialog(
      decompiledTreeProvider,
      decompiledTreeView
    )
  );
  disposables.push(
    registerDecompileSelectedAssembly(
      decompiledTreeProvider,
      decompiledTreeView
    )
  );

  const decompilerTextDocumentContentProvider =
    new DecompilerTextDocumentContentProvider(ilspyBackend);

  const searchResultTreeProvider = new SearchResultTreeProvider(ilspyBackend);
  const searchResultTreeView = createSearchResultTreeView(
    searchResultTreeProvider
  );
  disposables.push(searchResultTreeView);
  disposables.push(registerSearch(searchResultTreeProvider));

  disposables.push(
    workspace.registerTextDocumentContentProvider(
      ILSPY_URI_SCHEME,
      decompilerTextDocumentContentProvider
    )
  );

  disposables.push(
    registerDecompileNode(decompilerTextDocumentContentProvider)
  );

  disposables.push(
    registerSelectOutputLanguage(decompilerTextDocumentContentProvider)
  );
  disposables.push(
    ...registerSelectOutputLanguageStatusBarItem(
      decompilerTextDocumentContentProvider
    )
  );

  disposables.push(registerReloadAssembly(decompiledTreeProvider));
  disposables.push(registerUnloadAssembly(decompiledTreeProvider));

  disposables.push(registerSearchEditorSelection());

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
