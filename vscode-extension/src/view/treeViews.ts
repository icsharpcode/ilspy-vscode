import { TreeView, window } from "vscode";
import Node from "../protocol/Node";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { SearchResultTreeProvider } from "../decompiler/search/SearchResultTreeProvider";

export function createDecompiledTreeView(
  decompiledTreeProvider: DecompiledTreeProvider
): TreeView<Node> {
  return window.createTreeView("ilspyDecompiledMembers", {
    treeDataProvider: decompiledTreeProvider,
    showCollapseAll: true,
  });
}

export function createSearchResultTreeView(
  searchResultTreeProvider: SearchResultTreeProvider
): TreeView<Node> {
  return window.createTreeView("ilspySearchResultsContainer", {
    treeDataProvider: searchResultTreeProvider,
    showCollapseAll: true,
  });
}
