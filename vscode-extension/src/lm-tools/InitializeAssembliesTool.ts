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
} from "./lmToolsUtils";

interface IInitializeAssembliesToolParameters {
  assemblyPaths?: string[];
}

export class InitializeAssembliesTool implements vscode.LanguageModelTool<IInitializeAssembliesToolParameters> {
  static Name: string = "initialize_loaded_assemblies";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IInitializeAssembliesToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const assemblyPaths = options.input.assemblyPaths ?? [];
    if (assemblyPaths.length === 0) {
      throw new Error("At least one assembly path is required.");
    }

    const response = await this.ilspyBackend.sendInitWithAssemblies({
      assemblyPaths,
    });
    if (response?.loadedAssemblies) {
      await refreshAssemblyList();
    }

    return createJsonResult({
      requestedAssemblyPaths: assemblyPaths,
      loadedAssemblies: (response?.loadedAssemblies ?? []).map(
        summarizeAssemblyData,
      ),
      loadedCount: response?.loadedAssemblies?.length ?? 0,
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IInitializeAssembliesToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Initializing loaded assemblies",
      confirmationMessages: {
        title: "Initialize ILSpy assemblies",
        message: new vscode.MarkdownString(
          `Initialize ILSpy with ${(options.input.assemblyPaths ?? []).length} assembly path(s)?`,
        ),
      },
    };
  }
}
