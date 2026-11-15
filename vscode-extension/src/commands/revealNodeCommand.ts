/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { registerILSpyCommand } from "./commandUtils";
import { Node } from "../extension-types";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";

export function registerRevealNodeCommand(
  decompiledTreeView: vscode.TreeView<Node>,
  decompiledTreeProvider: DecompiledTreeProvider,
) {
  return registerILSpyCommand("ilspy.revealNode", async (node: Node) => {
    // Nodes from search or analyzer results may differ from the tree's own nodes (e.g. in their
    // display name), so reveal the matching tree node, or its closest visible ancestor.
    const treeNode = await decompiledTreeProvider.findTreeNode(node);
    if (!treeNode) {
      return;
    }

    await decompiledTreeView.reveal(treeNode, {
      expand: true,
      select: true,
      focus: true,
    });
  });
}
