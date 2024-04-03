import { TreeView, window } from "vscode";
import Node from "../protocol/Node";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { SearchResultTreeProvider } from "../decompiler/search/SearchResultTreeProvider";
import { AnalyzeResultTreeProvider } from "../decompiler/analyze/AnalyzeResultTreeProvider";

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

export function createAnalyzeResultTreeView(
  analyzeResultTreeProvider: AnalyzeResultTreeProvider
): TreeView<Node> {
  return window.createTreeView("ilspyAnalyzeResultsContainer", {
    treeDataProvider: analyzeResultTreeProvider,
    showCollapseAll: true,
  });
}