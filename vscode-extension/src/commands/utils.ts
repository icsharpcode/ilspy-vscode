/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as fs from "fs";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { Node, NodeType } from "../extension-types";

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
    const newNode = await decompiledTreeProvider.findNode(
      (node) => node.metadata?.assemblyPath === assembly
    );
    if (newNode) {
      decompiledTreeView.reveal(newNode, { focus: true, select: true });
    }

    return newNode;
  }

  return undefined;
}

export async function getOrPickAssemblyNode(
  node: Node | undefined,
  decompiledTreeProvider: DecompiledTreeProvider
) {
  if (node?.metadata?.type === NodeType.Assembly) {
    return node;
  }

  const assemblies = (await decompiledTreeProvider.getChildNodes()).filter(
    (n) => n.metadata?.type === NodeType.Assembly
  );
  if (assemblies.length === 0) {
    vscode.window.showWarningMessage("No assemblies loaded.");
    return undefined;
  }

  const pick = await vscode.window.showQuickPick(
    assemblies.map((a) => ({
      label: a.displayName,
      description: a.description,
      node: a,
    })),
    { title: "Select assembly to export" }
  );

  return pick?.node;
}
