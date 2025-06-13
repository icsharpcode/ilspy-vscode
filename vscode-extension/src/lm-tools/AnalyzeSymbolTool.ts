/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { AvailableNodeCommands } from "../protocol/AvailableNodeCommands";
import {
  createJsonResult,
  refreshAssemblyList,
  resolveSingleNode,
  summarizeNode,
  SymbolQueryInput,
} from "./toolUtils";

type IAnalyzeSymbolToolParameters = SymbolQueryInput;

export class AnalyzeSymbolTool implements vscode.LanguageModelTool<IAnalyzeSymbolToolParameters> {
  static Name: string = "analyze_symbol";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IAnalyzeSymbolToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const targetNode = await resolveSingleNode(
      this.ilspyBackend,
      options.input,
      "analyze",
      AvailableNodeCommands.Analyze,
    );

    const response = await this.ilspyBackend.sendAnalyze({
      nodeMetadata: targetNode.metadata,
    });
    if (response?.shouldUpdateAssemblyList) {
      await refreshAssemblyList();
    }

    return createJsonResult({
      target: summarizeNode(targetNode),
      results: (response?.results ?? []).map(summarizeNode),
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IAnalyzeSymbolToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Analyzing ILSpy symbol",
      confirmationMessages: {
        title: "Analyze symbol with ILSpy",
        message: new vscode.MarkdownString(`Analyze a loaded ILSpy symbol?`),
      },
    };
  }
}
