/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2025 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { addAssemblyFromFilePath } from "./utils";
import { Node } from "../extension-types";
import { registerILSpyCommand } from "./commandUtils";

export function registerAddAssemblyByPathCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<Node>,
) {
  return registerILSpyCommand(
    "ilspy.addAssemblyByPath",
    async (filePath: string) => {
      addAssemblyFromFilePath(
        filePath,
        decompiledTreeProvider,
        decompiledTreeView,
      );
    },
  );
}
