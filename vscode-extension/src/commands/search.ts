/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { SearchResultTreeProvider } from "../decompiler/search/SearchResultTreeProvider";

export function registerSearch(
  searchResultTreeProvider: SearchResultTreeProvider
) {
  return vscode.commands.registerCommand("ilspy.search", async () => {
    const searchTerm = await vscode.window.showInputBox({
      prompt: "Please enter the search term",
    });

    if (searchTerm) {
      vscode.commands.executeCommand(
        "setContext",
        "ilspy.searchResultsToShow",
        true
      );
      searchResultTreeProvider.performSearch(searchTerm);
      vscode.commands.executeCommand("ilspySearchResultsContainer.focus");
    }
  });
}
