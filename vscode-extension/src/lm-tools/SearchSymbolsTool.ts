/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import {
  AssemblyFilterInput,
  createJsonResult,
  searchNodes,
  summarizeNode,
} from "./lmToolsUtils";

interface ISearchSymbolsToolParameters extends AssemblyFilterInput {
  term?: string;
}

export class SearchSymbolsTool implements vscode.LanguageModelTool<ISearchSymbolsToolParameters> {
  static Name: string = "search_decompiled_symbols";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<ISearchSymbolsToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const term = options.input.term;
    if (typeof term !== "string" || term.trim().length === 0) {
      throw new Error("A search term is required.");
    }

    const results = await searchNodes(this.ilspyBackend, term, options.input);
    const maxResults = 50;

    return createJsonResult({
      term,
      totalResults: results.length,
      truncated: results.length > maxResults,
      results: results.slice(0, maxResults).map(summarizeNode),
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<ISearchSymbolsToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Searching ILSpy symbols",
      confirmationMessages: {
        title: "Search loaded ILSpy symbols",
        message: new vscode.MarkdownString(
          `Search symbols in loaded ILSpy assemblies?`,
        ),
      },
    };
  }
}
