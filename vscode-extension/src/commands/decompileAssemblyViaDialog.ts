/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { addAssemblyFromFilePath } from "./utils";
import { ASSEMBLY_FILE_EXTENSIONS } from "../decompiler/utils";
import { registerILSpyCommand } from "./registerILSpyCommand";
import { Node } from "../extension-types";

export function registerDecompileAssemblyViaDialogCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<Node>
) {
  return registerILSpyCommand(
    "ilspy.decompileAssemblyViaDialog",
    async () => {
      const files = await promptForAssemblyFilesPathViaDialog();
      files.forEach((file) => {
        addAssemblyFromFilePath(
          file,
          decompiledTreeProvider,
          decompiledTreeView
        );
      });
    }
  );
}

async function promptForAssemblyFilesPathViaDialog(): Promise<string[]> {
  const uris = await vscode.window.showOpenDialog({
    openLabel: "Select assemblies",
    canSelectFiles: true,
    canSelectFolders: false,
    canSelectMany: true,
    filters: {
      ".NET Decompilables": ASSEMBLY_FILE_EXTENSIONS,
    },
  });

  if (uris === undefined) {
    return [];
  }

  return uris.map((uri) => uri.fsPath);
}
