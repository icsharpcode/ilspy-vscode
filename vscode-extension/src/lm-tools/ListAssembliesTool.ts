/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import {
  createJsonResult,
  getAssemblyNodes,
  summarizeAssemblyNode,
} from "./lmToolsUtils";

export class ListAssembliesTool implements vscode.LanguageModelTool<{}> {
  static Name: string = "list_loaded_assemblies";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<{}>,
    token: vscode.CancellationToken,
  ) {
    const assemblies = await getAssemblyNodes(this.ilspyBackend);

    return createJsonResult({
      loadedAssemblies: assemblies.map(summarizeAssemblyNode),
      count: assemblies.length,
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<{}>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Listing loaded ILSpy assemblies",
      confirmationMessages: {
        title: "Get list of ILSpy assemblies",
        message: new vscode.MarkdownString(
          `List assemblies currently loaded in ILSpy?`,
        ),
      },
    };
  }
}
