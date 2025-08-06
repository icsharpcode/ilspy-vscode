/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { addAssemblyFromFilePath } from "./utils";
import Node from "../protocol/Node";

export function registerDecompileAssemblyViaDialogCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<Node>
) {
  return vscode.commands.registerCommand(
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
      ".NET Assemblies": ["dll", "exe", "winmd", "netmodule", "wasm"],
    },
  });

  if (uris === undefined) {
    return [];
  }

  return uris.map((uri) => uri.fsPath);
}
