/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import Node from "../protocol/Node";

export function registerRevealNodeCommand(
  decompiledTreeView: vscode.TreeView<Node>,
) {
  return vscode.commands.registerCommand(
    "ilspy.revealNode",
    async (node: Node) => {
      await decompiledTreeView.reveal(node, {
        expand: true,
        select: true,
        focus: true,
      });
    },
  );
}
