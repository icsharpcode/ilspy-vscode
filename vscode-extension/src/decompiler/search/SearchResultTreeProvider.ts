/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  TreeDataProvider,
  EventEmitter,
  TreeItem,
  Event,
  TreeItemCollapsibleState,
  ProviderResult,
  ThemeIcon,
  commands,
} from "vscode";
import IILSpyBackend from "../IILSpyBackend";
import { getNodeIcon } from "../../icons";
import { getShowCompilerGeneratedSymbolsSetting } from "../settings";
import {
  createNodeId,
  createNodeTooltip,
  getNodeContextValue,
  hasNodeCommand,
  hasNodeFlag,
} from "../utils";
import { AvailableNodeCommands, Node, NodeFlags } from "../../extension-types";
import { executeILSpyCommand } from "../../commands/commandUtils";

type PerformedSearch = {
  term: string;
  results: SearchResultNode[];
};

type SearchResultNode = Node & {
  searchIndex: number;
};

export type SearchTreeNode = SearchResultNode | PerformedSearch;

export function isPerformedSearchNode(
  node: SearchTreeNode
): node is PerformedSearch {
  return "term" in node && "results" in node;
}

export class SearchResultTreeProvider
  implements TreeDataProvider<SearchTreeNode>
{
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;
  private lastSearches: PerformedSearch[] = [];
  // Monotonic, so result node IDs stay unique after single searches are removed.
  private nextSearchIndex = 0;

  constructor(private backend: IILSpyBackend) {}

  public refresh(): void {
    this._onDidChangeTreeData.fire(null);
  }

  public async performSearch(term: string) {
    const searchResponse = await this.backend.sendSearch({ term });
    const newSearchIndex = this.nextSearchIndex++;
    this.lastSearches.push({
      term,
      results:
        searchResponse?.results.map((result) => ({
          ...result,
          searchIndex: newSearchIndex,
        })) ?? [],
    });
    this.searchesChanged();
    if (searchResponse?.shouldUpdateAssemblyList) {
      await executeILSpyCommand("ilspy.refreshAssemblyList");
    }
  }

  public removeSearch(search: PerformedSearch) {
    this.lastSearches = this.lastSearches.filter((s) => s !== search);
    this.searchesChanged();
  }

  public clearSearches() {
    this.lastSearches = [];
    this.searchesChanged();
  }

  private searchesChanged() {
    commands.executeCommand(
      "setContext",
      "ilspy.hasSearchResults",
      this.lastSearches.length > 0,
    );
    this.refresh();
  }

  public getTreeItem(node: SearchTreeNode): TreeItem {
    if (isPerformedSearchNode(node)) {
      return {
        label: `Search results for "${node.term}"`,
        collapsibleState: TreeItemCollapsibleState.Expanded,
        iconPath: new ThemeIcon("search-view-icon"),
        contextValue: "performedSearch",
      };
    } else {
      const item = {
        id: `${node.searchIndex}/${createNodeId(node)}`,
        label: node.displayName,
        description: node.description,
        tooltip: createNodeTooltip(node),
        collapsibleState: TreeItemCollapsibleState.None,
        command: {
          command: "ilspy.decompileNode",
          arguments: [node, true],
          title: "Decompile",
        },
        contextValue: getNodeContextValue(node),
        iconPath: getNodeIcon(node.metadata?.type),
      };
      return item;
    }
  }

  public findNode(predicate: (node: SearchTreeNode) => boolean) {
    return (this.getChildren() as SearchResultNode[]).find(predicate);
  }

  public getChildren(
    node?: SearchTreeNode
  ): SearchTreeNode[] | Thenable<SearchTreeNode[]> {
    if (!node) {
      return [...this.lastSearches].reverse();
    }

    if (isPerformedSearchNode(node)) {
      const showCompilerGeneratedSymbols =
        getShowCompilerGeneratedSymbolsSetting();
      return (
        node.results.filter(
          (node) =>
            showCompilerGeneratedSymbols ||
            !hasNodeFlag(node, NodeFlags.CompilerGenerated)
        ) ?? []
      );
    }

    return [];
  }

  public getParent?(element: SearchTreeNode): ProviderResult<SearchTreeNode> {
    // Note: This allows relealing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }
}
