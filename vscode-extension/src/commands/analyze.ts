/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2024 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { AnalyzeResultTreeProvider } from "../decompiler/analyze/AnalyzeResultTreeProvider";
import Node from "../protocol/Node";

export function registerAnalyze(
  analyzeResultTreeProvider: AnalyzeResultTreeProvider
) {
  return vscode.commands.registerCommand(
    "ilspy.analyze",
    async (node: Node) => {
      vscode.commands.executeCommand(
        "setContext",
        "ilspy.analyzeResultsToShow",
        true
      );
      analyzeResultTreeProvider.analyze(node);
      vscode.commands.executeCommand("ilspyAnalyzeResultsContainer.focus");
    }
  );
}
