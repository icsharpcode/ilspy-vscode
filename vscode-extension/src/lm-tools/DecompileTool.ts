/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import { AvailableNodeCommands } from "../protocol/AvailableNodeCommands";
import {
  createJsonResult,
  pickPreferredSymbolMatches,
  searchNodes,
  summarizeNode,
  SymbolQueryInput,
} from "./toolUtils";

interface IDecompileToolParameters extends SymbolQueryInput {
  outputLanguage?: string;
}

export class DecompileTool implements vscode.LanguageModelTool<IDecompileToolParameters> {
  static Name: string = "decompile_symbol";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IDecompileToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const symbol = options.input.symbol;
    if (typeof symbol !== "string" || symbol.trim().length === 0) {
      throw new Error("No decompilation is possible without a symbol name");
    }

    const matches = pickPreferredSymbolMatches(
      await searchNodes(this.ilspyBackend, symbol, options.input),
      symbol,
    ).filter(
      (node) =>
        node.metadata !== undefined &&
        (node.metadata.availableCommands & AvailableNodeCommands.Decompile) !==
          0,
    );

    if (matches.length === 0) {
      return createJsonResult({
        symbol,
        decompiledSymbols: [],
        message: `No decompilable symbol matched "${symbol}".`,
      });
    }

    if (matches.length > 5) {
      throw new Error(
        `"${symbol}" matched ${matches.length} decompilable symbols. ` +
          `Narrow the request with assemblyPath or assemblyName before decompiling.`,
      );
    }

    const outputLanguage =
      options.input.outputLanguage ?? getDefaultOutputLanguage();
    const decompiledSymbols = [];
    for (const match of matches) {
      const decompileResult = await this.ilspyBackend.sendDecompileNode({
        nodeMetadata: match.metadata!,
        outputLanguage,
      });

      decompiledSymbols.push({
        target: summarizeNode(match),
        outputLanguage,
        code: decompileResult?.decompiledCode,
      });
    }

    return createJsonResult({
      symbol,
      decompiledSymbols,
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IDecompileToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Decompiling symbol",
      confirmationMessages: {
        title: "Decompile symbol",
        message: new vscode.MarkdownString(
          `Decompile a .NET symbol with ILSpy?` +
            (options.input.symbol !== undefined
              ? ` "${options.input.symbol}"`
              : ""),
        ),
      },
    };
  }
}
