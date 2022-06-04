/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";

export function registerSearch(ilspyBackend: IILSpyBackend) {
  return vscode.commands.registerCommand("ilspy.search", async () => {
    const searchTerm = await vscode.window.showInputBox({
      prompt: "Please enter the search term",
    });

    const response = await ilspyBackend.sendSearch({
      term: searchTerm,
    });

    if (!response) {
      return;
    }

    const selectedNode = await vscode.window.showQuickPick(
      response?.results.map(
        (res) =>
          ({
            label: res.name,
            description: res.description,
          } as vscode.QuickPickItem)
      ),
      { canPickMany: false, matchOnDetail: false, matchOnDescription: false }
    );
  });
}
