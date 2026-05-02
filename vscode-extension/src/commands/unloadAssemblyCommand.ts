/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { registerILSpyCommand } from "./commandUtils";
import { Node } from "../extension-types";

export function registerUnloadAssemblyCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
) {
  return registerILSpyCommand("ilspy.unloadAssembly", async (node: Node) => {
    if (!node || !node.metadata) {
      vscode.window.showInformationMessage(
        'Please use context menu: right-click on the assembly node then select "Unload Assembly"',
      );
      return;
    }
    console.log("Unloading assembly " + node.metadata.name);
    await decompiledTreeProvider.removeAssembly(node.metadata.assemblyPath);
  });
}
