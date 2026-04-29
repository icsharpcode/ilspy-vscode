/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { registerILSpyCommand } from "./registerILSpyCommand";
import { Node } from "../extension-types";

export function registerRevealNodeCommand(
  decompiledTreeView: vscode.TreeView<Node>,
) {
  return registerILSpyCommand(
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
