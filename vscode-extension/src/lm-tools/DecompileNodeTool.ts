/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { AvailableNodeCommands } from "../protocol/AvailableNodeCommands";
import NodeMetadata from "../protocol/NodeMetadata";
import {
  createJsonResult,
  ensureNodeMetadataCan,
  refreshAssemblyList,
  requireNodeMetadata,
  summarizeNodeMetadata,
} from "./toolUtils";

interface IDecompileNodeToolParameters {
  nodeMetadata?: NodeMetadata;
  outputLanguage?: string;
}

export class DecompileNodeTool
  implements vscode.LanguageModelTool<IDecompileNodeToolParameters>
{
  static Name: string = "decompile_node";

  constructor(private ilspyBackend: IILSpyBackend) {}

  async invoke(
    options: vscode.LanguageModelToolInvocationOptions<IDecompileNodeToolParameters>,
    token: vscode.CancellationToken,
  ) {
    const nodeMetadata = requireNodeMetadata(
      options.input.nodeMetadata,
      "decompile a tree node",
    );
    ensureNodeMetadataCan(
      nodeMetadata,
      nodeMetadata.name,
      "decompile",
      AvailableNodeCommands.Decompile,
    );

    const outputLanguage =
      options.input.outputLanguage ?? getDefaultOutputLanguage();
    const response = await this.ilspyBackend.sendDecompileNode({
      nodeMetadata,
      outputLanguage,
    });
    if (response?.shouldUpdateAssemblyList) {
      await refreshAssemblyList();
    }

    return createJsonResult({
      target: summarizeNodeMetadata(nodeMetadata),
      outputLanguage,
      succeeded: response !== null && response !== undefined && !response.isError,
      code: response?.decompiledCode,
      errorMessage: response?.errorMessage,
    });
  }

  async prepareInvocation(
    options: vscode.LanguageModelToolInvocationPrepareOptions<IDecompileNodeToolParameters>,
    token: vscode.CancellationToken,
  ) {
    return {
      invocationMessage: "Decompiling ILSpy node",
      confirmationMessages: {
        title: "Decompile ILSpy node",
        message: new vscode.MarkdownString(
          `Decompile an ILSpy tree node?` +
            (options.input.nodeMetadata?.name !== undefined
              ? `\n\n${options.input.nodeMetadata.name}`
              : ""),
        ),
      },
    };
  }
}
