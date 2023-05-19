/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as fs from "fs";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import Node from "../protocol/Node";

export function addAssemblyFromFilePath(
  filePath: string,
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<Node>
) {
  let assemblyFile = filePath;
  if (
    assemblyFile[0] === '"' &&
    assemblyFile[assemblyFile.length - 1] === '"'
  ) {
    assemblyFile = assemblyFile.slice(1, -1);
  }

  try {
    fs.accessSync(assemblyFile, fs.constants.R_OK);
    addAssemblyToTree(assemblyFile, decompiledTreeProvider, decompiledTreeView);
  } catch (err) {
    vscode.window.showErrorMessage("Cannot read the file " + filePath);
  }
}

export async function addAssemblyToTree(
  assembly: string,
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<Node>
) {
  const added = await decompiledTreeProvider.addAssembly(assembly);
  if (added) {
    const newNode = decompiledTreeProvider.findNode(
      (node) => node.metadata?.assemblyPath === assembly
    );
    if (newNode) {
      decompiledTreeView.reveal(newNode, { focus: true, select: true });
    }
  }
}
