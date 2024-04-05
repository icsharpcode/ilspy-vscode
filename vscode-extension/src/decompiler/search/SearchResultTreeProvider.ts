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
} from "vscode";
import IILSpyBackend from "../IILSpyBackend";
import Node from "../../protocol/Node";
import { getNodeIcon } from "../../icons";

interface PerformedSearch {
  term: string;
  results: Node[];
}

export type SearchTreeNode = Node | PerformedSearch;

export class SearchResultTreeProvider
  implements TreeDataProvider<SearchTreeNode>
{
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;
  private lastSearches: PerformedSearch[] = [];

  constructor(private backend: IILSpyBackend) {}

  public refresh(): void {
    this._onDidChangeTreeData.fire(null);
  }

  public async performSearch(term: string) {
    this.lastSearches.push({
      term,
      results: (await this.backend.sendSearch({ term }))?.results ?? [],
    });
    this.refresh();
  }

  public getTreeItem(element: SearchTreeNode): TreeItem {
    if ((element as PerformedSearch).term) {
      const performedSearch = element as PerformedSearch;
      return {
        label: `Search results for "${performedSearch.term}"`,
        collapsibleState: TreeItemCollapsibleState.Expanded,
        iconPath: new ThemeIcon("search-view-icon"),
      };
    } else {
      const nodeData = element as Node;
      return {
        label: nodeData.displayName,
        tooltip: nodeData.description,
        collapsibleState: TreeItemCollapsibleState.None,
        command: {
          command: "decompileNode",
          arguments: [nodeData],
          title: "Decompile",
        },
        iconPath: new ThemeIcon(getNodeIcon(nodeData.metadata?.type)),
      };
    }
  }

  public findNode(predicate: (node: SearchTreeNode) => boolean) {
    return (this.getChildren() as Node[]).find(predicate);
  }

  public getChildren(
    element?: SearchTreeNode
  ): SearchTreeNode[] | Thenable<SearchTreeNode[]> {
    if (!element) {
      return [...this.lastSearches].reverse();
    }

    if ((element as PerformedSearch).term) {
      return (element as PerformedSearch).results;
    }

    return [];
  }

  public getParent?(element: SearchTreeNode): ProviderResult<SearchTreeNode> {
    // Note: This allows relealing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }
}
