/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "./IILSpyBackend";
import { DecompilerTextDocumentContentProvider } from "./DecompilerTextDocumentContentProvider";
import { ILSPY_URI_SCHEME, nodeDataToUri, uriToNode } from "./nodeUri";
import { languageInfos } from "./languageInfos";
import { executeILSpyCommand } from "../commands/commandUtils";
import { LanguageName } from "../extension-types";
import {
  CodeLocation,
  CodeLocationsResponse,
  SymbolNavigationParams,
} from "../protocol/symbolNavigation";

/**
 * "Go to Definition" (F12, Ctrl/Cmd+Click) and "Go to Implementation" (Ctrl/Cmd+F12) in decompiled code.
 * The backend resolves the symbol at the position, the target is shown in the document of its tree node.
 */
export class SymbolNavigationProvider
  implements vscode.DefinitionProvider, vscode.ImplementationProvider
{
  constructor(
    private backend: IILSpyBackend,
    private contentProvider: DecompilerTextDocumentContentProvider,
  ) {}

  provideDefinition(
    document: vscode.TextDocument,
    position: vscode.Position,
    token: vscode.CancellationToken,
  ) {
    return this.findLocations(document, position, token, (params) =>
      this.backend.sendFindDefinitions(params, token),
    );
  }

  provideImplementation(
    document: vscode.TextDocument,
    position: vscode.Position,
    token: vscode.CancellationToken,
  ) {
    return this.findLocations(document, position, token, (params) =>
      this.backend.sendFindImplementations(params, token),
    );
  }

  private async findLocations(
    document: vscode.TextDocument,
    position: vscode.Position,
    token: vscode.CancellationToken,
    sendRequest: (
      params: SymbolNavigationParams,
    ) => Promise<CodeLocationsResponse | null>,
  ): Promise<vscode.Location[] | undefined> {
    const nodeMetadata = uriToNode(document.uri);
    if (!nodeMetadata) {
      return undefined;
    }

    const outputLanguage = this.contentProvider.getDocumentOutputLanguage(
      document.uri,
    );
    let response: CodeLocationsResponse | null;
    try {
      response = await sendRequest({
        nodeMetadata,
        outputLanguage,
        line: position.line,
        column: position.character,
      });
    } catch {
      // Cancelled (e.g. the cursor moved on) or failed, there's nothing to navigate to
      return undefined;
    }
    if (!response || token.isCancellationRequested) {
      return undefined;
    }

    if (response.shouldUpdateAssemblyList) {
      executeILSpyCommand("ilspy.refreshAssemblyList");
    }

    return response.locations?.map((location) =>
      this.toLocation(document.uri, location, outputLanguage),
    );
  }

  private toLocation(
    documentUri: vscode.Uri,
    location: CodeLocation,
    outputLanguage: LanguageName,
  ) {
    let uri = documentUri;
    if (location.node) {
      uri = nodeDataToUri(location.node);
      // The location refers to code decompiled in the same language as the requesting document
      this.contentProvider.initDocumentOutputLanguage(uri, outputLanguage);
    }

    return new vscode.Location(
      uri,
      new vscode.Range(
        location.line,
        location.column,
        location.line,
        location.column + location.length,
      ),
    );
  }
}

export function registerSymbolNavigationProvider(
  backend: IILSpyBackend,
  contentProvider: DecompilerTextDocumentContentProvider,
): vscode.Disposable[] {
  const selector: vscode.DocumentSelector = { scheme: ILSPY_URI_SCHEME };
  const provider = new SymbolNavigationProvider(backend, contentProvider);
  return [
    vscode.languages.registerDefinitionProvider(selector, provider),
    vscode.languages.registerImplementationProvider(selector, provider),
    // Documents opened by navigation (or restored on startup) don't get their language mode
    // from the "Decompile" command, so set it here for syntax highlighting.
    vscode.workspace.onDidOpenTextDocument((document) =>
      setDecompiledDocumentLanguageMode(document, contentProvider),
    ),
  ];
}

function setDecompiledDocumentLanguageMode(
  document: vscode.TextDocument,
  contentProvider: DecompilerTextDocumentContentProvider,
) {
  if (document.uri.scheme !== ILSPY_URI_SCHEME) {
    return;
  }

  const languageMode =
    languageInfos[contentProvider.getDocumentOutputLanguage(document.uri)]
      ?.vsLanguageMode;
  if (languageMode && document.languageId !== languageMode) {
    vscode.languages.setTextDocumentLanguage(document, languageMode);
  }
}
