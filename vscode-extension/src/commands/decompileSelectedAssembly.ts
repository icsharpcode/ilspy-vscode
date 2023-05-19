import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { addAssemblyToTree } from "./utils";
import { MemberNode } from "../decompiler/MemberNode";

export function registerDecompileSelectedAssembly(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<MemberNode>
) {
  return vscode.commands.registerCommand(
    "ilspy.decompileSelectedAssembly",
    async (file) => {
      if (file) {
        await addAssemblyToTree(
          file.path,
          decompiledTreeProvider,
          decompiledTreeView
        );
      }
    }
  );
}
