import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { addAssemblyToTree } from "./utils";
import { registerILSpyCommand } from "./registerILSpyCommand";
import { Node } from "../extension-types";

export function registerDecompileSelectedAssemblyCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<Node>
) {
  return registerILSpyCommand(
    "ilspy.decompileSelectedAssembly",
    async (file) => {
      if (file) {
        return await addAssemblyToTree(
          file.fsPath,
          decompiledTreeProvider,
          decompiledTreeView
        );
      }
    }
  );
}
