/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import {
  createJsonResult,
  refreshAssemblyList,
  summarizeAssemblyData,
} from "./toolUtils";

interface IAddAssemblyToolParameters {
  assemblyPath?: string;
}

export class AddAssemblyTool implements vscode.LanguageModelTool<IAddAssemblyToolParameters> {
  static Name: string = "add_loaded_assembly";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IAddAssemblyToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const assemblyPath = options.input.assemblyPath;
    if (typeof assemblyPath !== "string" || assemblyPath.trim().length === 0) {
      throw new Error("An assemblyPath is required.");
    }

    const response = await this.ilspyBackend.sendAddAssembly({ assemblyPath });
    if (response?.added) {
      await refreshAssemblyList();
    }

    return createJsonResult({
      assemblyPath,
      added: response?.added ?? false,
      assembly: response?.assemblyData
        ? summarizeAssemblyData(response.assemblyData)
        : undefined,
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IAddAssemblyToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Adding assembly to ILSpy",
      confirmationMessages: {
        title: "Add assembly to ILSpy",
        message: new vscode.MarkdownString(
          `Add assembly to ILSpy?` +
            (options.input.assemblyPath !== undefined
              ? `\n\n${options.input.assemblyPath}`
              : ""),
        ),
      },
    };
  }
}
