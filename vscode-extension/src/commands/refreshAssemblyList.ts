/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2025 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";

export function registerRefreshAssemblyListCommand(
  decompiledTreeProvider: DecompiledTreeProvider
) {
  return vscode.commands.registerCommand(
    "ilspy.refreshAssemblyList",
    async () => {
      await decompiledTreeProvider.refresh();
    }
  );
}
