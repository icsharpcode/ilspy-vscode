/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as path from "path";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { addAssemblyToTree } from "./utils";
import { MemberNode } from "../decompiler/MemberNode";

export function registerDecompileAssemblyInWorkspace(
  decompiledTreeProvider: DecompiledTreeProvider,
  decompiledTreeView: vscode.TreeView<MemberNode>
) {
  return vscode.commands.registerCommand(
    "ilspy.decompileAssemblyInWorkspace",
    async () => {
      // The code you place here will be executed every time your command is executed
      const assembly = await pickAssembly();
      if (assembly) {
        await addAssemblyToTree(
          assembly.assemblyPath,
          decompiledTreeProvider,
          decompiledTreeView
        );
      }
    }
  );
}

async function pickAssembly(): Promise<AssemblyQuickPickItem | undefined> {
  const assemblies = await findAssemblies();
  const assemblyPathInfo: AssemblyPathInfo[] = parseAssemblyPath(assemblies);
  const quickPickItems = assemblyPathInfo.map((info) =>
    createAssemblyQuickPickItem(info)
  );
  if (quickPickItems.length === 0) {
    vscode.window.showInformationMessage("No assembly found inside the workspace");
  } else {
    return await vscode.window.showQuickPick<AssemblyQuickPickItem>(
      quickPickItems
    );
  }
}

function parseAssemblyPath(assemblies: string[]): AssemblyPathInfo[] {
  const workspaceFolders = vscode.workspace.workspaceFolders;
  return assemblies.map((assemblyPath) => {
    const p = path.parse(assemblyPath);
    const assemblyWorkspace = workspaceFolders?.find((w) =>
      p.dir.includes(w.uri.fsPath)
    );
    return {
      fileName: p.base,
      fileExtension: p.ext,
      fullPath: assemblyPath,
      relativePath: p.dir.replace(assemblyWorkspace?.uri.fsPath ?? "", ""),
      workspaceFolder: assemblyWorkspace?.name,
    };
  });
}

async function findAssemblies(): Promise<string[]> {
  if (!vscode.workspace.workspaceFolders) {
    return Promise.resolve([]);
  }

  const resources = await vscode.workspace.findFiles(
    /*include*/ "{**/*.dll,**/*.exe,**/*.winmd,**/*.netmodule}",
    /*exclude*/ "{**/node_modules/**,**/.git/**,**/bower_components/**}"
  );
  return resources.map((uri) => uri.fsPath).sort((s1, s2) => {
    if (s1 > s2) {
      return 1;
    } else if (s1 < s2) {
      return -1;
    } else {
      return 0;
    }
  });
}

function createAssemblyQuickPickItem(
  assemblyPathInfo: AssemblyPathInfo
): AssemblyQuickPickItem {
  const selectIcon = (extension: string) => {
    switch (extension) {
      case ".dll":
      case ".winmd":
      case ".netmodule":
        return "library";
      case ".exe":
        return "file-binary";
      default:
        return "file";
    }
  };
  const res: AssemblyQuickPickItem = {
    label: `$(${selectIcon(assemblyPathInfo.fileExtension)}) ${
      assemblyPathInfo.fileName
    }`,
    description: assemblyPathInfo.fullPath,
    detail: path.join(
      assemblyPathInfo.workspaceFolder ?? "",
      assemblyPathInfo.relativePath
    ),
    assemblyPath: assemblyPathInfo.fullPath,
  };
  return res;
}

interface AssemblyPathInfo {
  fullPath: string;
  relativePath: string;
  fileName: string;
  fileExtension: string;
  workspaceFolder?: string;
}

interface AssemblyQuickPickItem extends vscode.QuickPickItem {
  assemblyPath: string;
}
