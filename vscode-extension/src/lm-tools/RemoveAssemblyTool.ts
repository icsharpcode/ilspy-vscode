/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import {
  AssemblyFilterInput,
  createJsonResult,
  refreshAssemblyList,
  resolveAssemblyNode,
} from "./toolUtils";

type IRemoveAssemblyToolParameters = AssemblyFilterInput;

export class RemoveAssemblyTool implements vscode.LanguageModelTool<IRemoveAssemblyToolParameters> {
  static Name: string = "remove_loaded_assembly";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IRemoveAssemblyToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const assemblyNode = await resolveAssemblyNode(
      this.ilspyBackend,
      options.input,
    );
    const assemblyPath = assemblyNode.metadata?.assemblyPath;
    if (!assemblyPath) {
      throw new Error("The selected assembly has no assemblyPath.");
    }

    const response = await this.ilspyBackend.sendRemoveAssembly({
      assemblyPath,
    });
    if (response?.removed) {
      await refreshAssemblyList();
    }

    return createJsonResult({
      assemblyPath,
      removed: response?.removed ?? false,
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IRemoveAssemblyToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Removing assembly from ILSpy",
      confirmationMessages: {
        title: "Remove assembly from ILSpy",
        message: new vscode.MarkdownString(
          `Remove a loaded assembly from ILSpy?`,
        ),
      },
    };
  }
}
