/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2024 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import {
  AnalyzeResultTreeProvider,
  AnalyzeTreeNode,
} from "../decompiler/analyze/AnalyzeResultTreeProvider";
import { Node } from "../extension-types";
import { registerILSpyCommand } from "./commandUtils";

export function registerAnalyzeCommand(
  analyzeResultTreeProvider: AnalyzeResultTreeProvider,
  analyzeResultTreeView: vscode.TreeView<AnalyzeTreeNode>,
) {
  return registerILSpyCommand("ilspy.analyze", async (node: Node) => {
    vscode.commands.executeCommand(
      "setContext",
      "ilspy.analyzeResultsToShow",
      true,
    );
    await analyzeResultTreeProvider.analyze(node);
    vscode.commands.executeCommand("ilspyAnalyzeResultsContainer.focus");

    const firstNode = analyzeResultTreeProvider.getFirstNode();
    if (firstNode) {
      analyzeResultTreeView.reveal(firstNode);
    }
  });
}
