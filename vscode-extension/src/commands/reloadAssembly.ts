/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import Node from "../protocol/Node";

export function registerReloadAssembly(
  decompiledTreeProvider: DecompiledTreeProvider
) {
  return vscode.commands.registerCommand(
    "ilspy.reloadAssembly",
    async (node: Node) => {
      if (!node || !node.metadata) {
        vscode.window.showInformationMessage(
          'Please use context menu: right-click on the assembly node then select "Reload Assembly"'
        );
        return;
      }
      await decompiledTreeProvider.reloadAssembly(node.metadata.assemblyPath);
    }
  );
}
