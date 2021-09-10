/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { MemberNode } from "../decompiler/MemberNode";
import { addAssemblyFromFilePath } from "./utils";

export function registerDecompileAssemblyViaDialog(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<MemberNode>
) {
  return vscode.commands.registerCommand(
    "ilspy.decompileAssemblyViaDialog",
    async () => {
      const file = await promptForAssemblyFilePathViaDialog();
      if (file) {
        addAssemblyFromFilePath(
          file,
          decompiledTreeProvider,
          decompiledTreeView
        );
      }
    }
  );
}

async function promptForAssemblyFilePathViaDialog(): Promise<
  string | undefined
> {
  const uris = await vscode.window.showOpenDialog(
    /* options*/ {
      openLabel: "Select assembly",
      canSelectFiles: true,
      canSelectFolders: false,
      canSelectMany: false,
      filters: {
        ".NET Assemblies": ["dll", "exe", "winrt", "netmodule"],
      },
    }
  );

  if (uris === undefined) {
    return undefined;
  }

  let strings = uris.map((uri) => uri.fsPath);
  if (strings.length > 0) {
    return strings[0];
  } else {
    return undefined;
  }
}
