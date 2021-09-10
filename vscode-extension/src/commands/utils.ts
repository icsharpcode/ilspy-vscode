/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as fs from "fs";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { MemberNode } from "../decompiler/MemberNode";

export function addAssemblyFromFilePath(
  filePath: string,
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<MemberNode>
) {
  let escaped: string = filePath.replace(/\\/g, "\\\\");
  if (escaped[0] === '"' && escaped[escaped.length - 1] === '"') {
    escaped = escaped.slice(1, -1);
  }

  try {
    fs.accessSync(escaped, fs.constants.R_OK);
    addAssemblyToTree(escaped, decompiledTreeProvider, decompiledTreeView);
  } catch (err) {
    vscode.window.showErrorMessage("Cannot read the file " + filePath);
  }
}

export async function addAssemblyToTree(
  assembly: string,
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<MemberNode>
) {
  const added = await decompiledTreeProvider.addAssembly(assembly);
  if (added) {
    const newNode = decompiledTreeProvider.findNode(
      (node) => node.assembly === assembly
    );
    if (newNode) {
      decompiledTreeView.reveal(newNode, { focus: true, select: true });
    }
  }
}
