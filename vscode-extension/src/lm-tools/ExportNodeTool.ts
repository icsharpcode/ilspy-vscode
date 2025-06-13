/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import {
  getDefaultOutputLanguage,
  getShowCompilerGeneratedSymbolsSetting,
} from "../decompiler/settings";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { AvailableNodeCommands } from "../protocol/AvailableNodeCommands";
import {
  createJsonResult,
  refreshAssemblyList,
  resolveSingleNode,
  summarizeNode,
  SymbolQueryInput,
} from "./toolUtils";

interface IExportNodeToolParameters extends SymbolQueryInput {
  outputDirectory?: string;
  outputLanguage?: string;
  includeCompilerGenerated?: boolean;
}

export class ExportNodeTool implements vscode.LanguageModelTool<IExportNodeToolParameters> {
  static Name: string = "export_decompiled_code";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IExportNodeToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const outputDirectory = options.input.outputDirectory;
    if (
      typeof outputDirectory !== "string" ||
      outputDirectory.trim().length === 0
    ) {
      throw new Error("An outputDirectory is required.");
    }

    const targetNode = await resolveSingleNode(
      this.ilspyBackend,
      options.input,
      "export",
      AvailableNodeCommands.Export,
    );
    const response = await this.ilspyBackend.sendExportAssembly(
      {
        nodeMetadata: targetNode.metadata!,
        outputLanguage:
          options.input.outputLanguage ?? getDefaultOutputLanguage(),
        outputDirectory,
        includeCompilerGenerated:
          options.input.includeCompilerGenerated ??
          getShowCompilerGeneratedSymbolsSetting(),
      },
      token,
    );
    if (response?.shouldUpdateAssemblyList) {
      await refreshAssemblyList();
    }

    return createJsonResult({
      target: summarizeNode(targetNode),
      succeeded: response?.succeeded ?? false,
      outputDirectory: response?.outputDirectory ?? outputDirectory,
      filesWritten: response?.filesWritten ?? 0,
      errorCount: response?.errorCount ?? 0,
      errorMessage: response?.errorMessage,
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IExportNodeToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Exporting decompiled code",
      confirmationMessages: {
        title: "Export decompiled ILSpy code",
        message: new vscode.MarkdownString(
          `Export decompiled code to disk?` +
            (options.input.outputDirectory !== undefined
              ? `\n\n${options.input.outputDirectory}`
              : ""),
        ),
      },
    };
  }
}
