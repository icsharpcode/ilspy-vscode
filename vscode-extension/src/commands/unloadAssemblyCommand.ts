/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { hasNodeCommand } from "../decompiler/utils";
import { registerILSpyCommand } from "./commandUtils";
import { AvailableNodeCommands, Node } from "../extension-types";

export function registerUnloadAssemblyCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
) {
  return registerILSpyCommand("ilspy.unloadAssembly", async (node: Node) => {
    const nodeToUnload =
      node?.metadata !== undefined
        ? node
        : await pickAssemblyToUnload(decompiledTreeProvider);
    if (!nodeToUnload?.metadata) {
      return;
    }
    console.log("Unloading assembly " + nodeToUnload.metadata.name);
    await decompiledTreeProvider.removeAssembly(
      nodeToUnload.metadata.assemblyPath,
    );
  });
}

async function pickAssemblyToUnload(
  decompiledTreeProvider: DecompiledTreeProvider,
): Promise<Node | undefined> {
  const unloadableNodes = (await decompiledTreeProvider.getChildNodes()).filter(
    (node) => hasNodeCommand(node, AvailableNodeCommands.ManageRootEntries),
  );
  if (unloadableNodes.length === 0) {
    vscode.window.showInformationMessage("No assemblies loaded in ILSpy.");
    return undefined;
  }

  const picked = await vscode.window.showQuickPick(
    unloadableNodes.map((node) => ({
      label: node.displayName,
      description: node.metadata?.assemblyPath,
      node,
    })),
    { placeHolder: "Select the assembly or package to unload" },
  );
  return picked?.node;
}
