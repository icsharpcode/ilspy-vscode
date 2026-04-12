/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import NodeMetadata from "../protocol/NodeMetadata";
import {
  createJsonResult,
  getRootNodes,
  hasAssemblyFilter,
  refreshAssemblyList,
  requireNodeMetadata,
  resolveSingleNode,
  summarizeNode,
  summarizeNodeMetadata,
  SymbolQueryInput,
} from "./lmToolsUtils";

interface IListNodesToolParameters extends SymbolQueryInput {
  nodeMetadata?: NodeMetadata;
}

export class ListNodesTool implements vscode.LanguageModelTool<IListNodesToolParameters> {
  static Name: string = "list_nodes";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IListNodesToolParameters>,
    token: vscode.CancellationToken,
  ) {
    if (options.input.nodeMetadata !== undefined) {
      const nodeMetadata = requireNodeMetadata(
        options.input.nodeMetadata,
        "list child nodes for a tree node",
      );
      const response = await this.ilspyBackend.sendGetNodes({
        nodeMetadata,
      });
      if (response?.shouldUpdateAssemblyList) {
        await refreshAssemblyList();
      }

      return createJsonResult({
        target: summarizeNodeMetadata(nodeMetadata),
        nodes: (response?.nodes ?? []).map(summarizeNode),
      });
    }

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
