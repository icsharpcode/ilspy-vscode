import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { addAssemblyToTree } from "./utils";
import Node from "../protocol/Node";

export function registerDecompileSelectedAssemblyCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<Node>
) {
  return vscode.commands.registerCommand(
    "ilspy.decompileSelectedAssembly",
    async (file) => {
      if (file) {
        await addAssemblyToTree(
          file.fsPath,
          decompiledTreeProvider,
          decompiledTreeView
        );
      }
    }
  );
}
