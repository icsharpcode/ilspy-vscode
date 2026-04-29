/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { SearchResultTreeProvider } from "../decompiler/search/SearchResultTreeProvider";
import { registerILSpyCommand } from "./commandUtils";
import { Node } from "../extension-types";

export function registerSearchCommand(
  searchResultTreeProvider: SearchResultTreeProvider,
) {
  return registerILSpyCommand("ilspy.search", async (term?: string | Node) => {
    const searchTerm =
      typeof term === "string"
        ? term
        : await vscode.window.showInputBox({
            prompt: "Please enter the search term",
          });

    if (searchTerm) {
      vscode.commands.executeCommand(
        "setContext",
        "ilspy.searchResultsToShow",
        true,
      );
      searchResultTreeProvider.performSearch(searchTerm);
      vscode.commands.executeCommand("ilspySearchResultsContainer.focus");
    }
  });
}
