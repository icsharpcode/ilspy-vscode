/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  isPerformedSearchNode,
  SearchResultTreeProvider,
  SearchTreeNode,
} from "../decompiler/search/SearchResultTreeProvider";
import { registerILSpyCommand } from "./commandUtils";

export function registerClearSearchResultCommand(
  searchResultTreeProvider: SearchResultTreeProvider,
) {
  return registerILSpyCommand(
    "ilspy.clearSearchResult",
    async (search: { term: string }) => {
      const node = search as SearchTreeNode | undefined;
      if (node && isPerformedSearchNode(node)) {
        searchResultTreeProvider.removeSearch(node);
      }
    },
  );
}

export function registerClearAllSearchResultsCommand(
  searchResultTreeProvider: SearchResultTreeProvider,
) {
  return registerILSpyCommand("ilspy.clearAllSearchResults", async () => {
    searchResultTreeProvider.clearSearches();
  });
}
