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
import { registerDecompileAssemblyInWorkspaceCommand } from "./commands/decompileAssemblyInWorkspace";
import { registerDecompileAssemblyViaDialogCommand } from "./commands/decompileAssemblyViaDialog";
import { registerDecompileSelectedAssemblyCommand } from "./commands/decompileSelectedAssembly";
import { registerReloadAssemblyCommand } from "./commands/reloadAssembly";
import { registerUnloadAssemblyCommand } from "./commands/unloadAssembly";
import { resolveDotnetRuntime } from "./dotnet-acquire/resolveDotnetRuntime";
import OutputWindowLogger from "./OutputWindowLogger";
import {
  commands,
  Disposable,
  ExtensionContext,
  window,
  workspace,
  lm
} from "vscode";
import { DecompilerTextDocumentContentProvider } from "./decompiler/DecompilerTextDocumentContentProvider";
import {
  registerSelectOutputLanguageCommand,
  registerSelectOutputLanguageStatusBarItem,
} from "./commands/selectOutputLanguage";
import { ILSPY_URI_SCHEME } from "./decompiler/nodeUri";
import { registerSearchCommand } from "./commands/search";
import { SearchResultTreeProvider } from "./decompiler/search/SearchResultTreeProvider";
import { registerDecompileNodeCommand } from "./commands/decompileNode";
import {
  createAnalyzeResultTreeView,
  createDecompiledTreeView,
  createSearchResultTreeView,
} from "./view/treeViews";
import { registerSearchEditorSelectionCommand } from "./commands/searchEditorSelection";
import { AnalyzeResultTreeProvider } from "./decompiler/analyze/AnalyzeResultTreeProvider";
import { registerAnalyzeCommand } from "./commands/analyze";
import { registerRefreshAssemblyListCommand } from "./commands/refreshAssemblyList";
import { AssemblyNodeDecorationProvider } from "./decompiler/AssemblyNodeDecorationProvider";

import { registerAddAssemblyTool } from "./tools/addAssembly";
import { registerDecompileNodeTool } from "./tools/decompileNode";
import { registerLoadedAssembliesTool } from "./tools/getLoadedAssemblies";
import { registerNavigateDefinitionTool } from "./tools/navigateDefinition";
import { registerOpenAnalyzePanelTool } from "./tools/openAnalyzePanel";
import { registerOpenDecompiledSourceTool } from "./tools/openDecompiledSource";
import { registerSearchAndOpenTool } from "./tools/searchAndOpen";
import { registerSearchAssembliesTool } from "./tools/searchAssemblies";

let client: LanguageClient;

export async function activate(context: ExtensionContext) {
  const disposables: Disposable[] = [];

  const logger = new OutputWindowLogger();

  setBackendAvailable(false);

  const dotnetCli = await resolveDotnetRuntime(context, logger);
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

  const assemblyNodeDecorationProvider = new AssemblyNodeDecorationProvider();
  disposables.push(
    window.registerFileDecorationProvider(assemblyNodeDecorationProvider)
  );

  disposables.push(
    registerDecompileAssemblyInWorkspaceCommand(
      decompiledTreeProvider,
      decompiledTreeView
    )
  );
  disposables.push(
    registerDecompileAssemblyViaDialogCommand(
      decompiledTreeProvider,
      decompiledTreeView
    )
  );
  disposables.push(
    registerDecompileSelectedAssemblyCommand(
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
  disposables.push(registerSearchCommand(searchResultTreeProvider));

  const analyzeResultTreeProvider = new AnalyzeResultTreeProvider(ilspyBackend);
  const analyzeResultTreeView = createAnalyzeResultTreeView(
    analyzeResultTreeProvider
  );
  disposables.push(analyzeResultTreeView);
  disposables.push(
    registerAnalyzeCommand(analyzeResultTreeProvider, analyzeResultTreeView)
  );

  disposables.push(
    workspace.registerTextDocumentContentProvider(
      ILSPY_URI_SCHEME,
      decompilerTextDocumentContentProvider
    )
  );

  disposables.push(
    registerDecompileNodeCommand(decompilerTextDocumentContentProvider)
  );

  disposables.push(
    registerSelectOutputLanguageCommand(decompilerTextDocumentContentProvider)
  );
  disposables.push(
    ...registerSelectOutputLanguageStatusBarItem(
      decompilerTextDocumentContentProvider
    )
  );

  disposables.push(registerReloadAssemblyCommand(decompiledTreeProvider));
  disposables.push(registerUnloadAssemblyCommand(decompiledTreeProvider));
  disposables.push(registerRefreshAssemblyListCommand(decompiledTreeProvider));

  disposables.push(registerSearchEditorSelectionCommand());

  // Register Language Model Tools for AI Agents
  disposables.push(
    registerAddAssemblyTool(decompiledTreeProvider),
    registerSearchAssembliesTool(ilspyBackend),
    registerDecompileNodeTool(ilspyBackend),
    registerLoadedAssembliesTool(decompiledTreeProvider),
    registerNavigateDefinitionTool(ilspyBackend),
    registerOpenDecompiledSourceTool(decompilerTextDocumentContentProvider),
    registerOpenAnalyzePanelTool(analyzeResultTreeProvider),
    registerSearchAndOpenTool(ilspyBackend, decompilerTextDocumentContentProvider)
  );

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
