/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import {
  createJsonResult,
  getRootNodes,
  hasAssemblyFilter,
  refreshAssemblyList,
  resolveSingleNode,
  summarizeNode,
  SymbolQueryInput,
} from "./toolUtils";

type IListNodesToolParameters = SymbolQueryInput;

export class ListNodesTool implements vscode.LanguageModelTool<IListNodesToolParameters> {
  static Name: string = "list_decompiler_nodes";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IListNodesToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const hasTarget =
      (options.input.symbol?.trim().length ?? 0) > 0 ||
      hasAssemblyFilter(options.input);

    if (!hasTarget) {
      const nodes = await getRootNodes(this.ilspyBackend);
      return createJsonResult({
        scope: "root",
        nodes: nodes.map(summarizeNode),
      });
    }

    const targetNode = await resolveSingleNode(
      this.ilspyBackend,
      options.input,
      "list child nodes",
    );
    const response = await this.ilspyBackend.sendGetNodes({
      nodeMetadata: targetNode.metadata,
    });
    if (response?.shouldUpdateAssemblyList) {
      await refreshAssemblyList();
    }

    return createJsonResult({
      target: summarizeNode(targetNode),
      nodes: (response?.nodes ?? []).map(summarizeNode),
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IListNodesToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Listing ILSpy nodes",
      confirmationMessages: {
        title: "List ILSpy nodes",
        message: new vscode.MarkdownString(
          `List nodes from the ILSpy decompiler tree?`,
        ),
      },
    };
  }
}
