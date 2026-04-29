/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { registerILSpyCommand } from "./registerILSpyCommand";
import { Node } from "../extension-types";

export function registerReloadAssemblyCommand(
  decompiledTreeProvider: DecompiledTreeProvider
) {
  return registerILSpyCommand(
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
