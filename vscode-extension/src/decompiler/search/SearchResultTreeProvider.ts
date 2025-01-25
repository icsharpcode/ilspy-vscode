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
import Node from "../../protocol/Node";
import { getNodeIcon } from "../../icons";

interface PerformedSearch {
  term: string;
  results: Node[];
}

export type SearchTreeNode = Node | PerformedSearch;

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

  constructor(private backend: IILSpyBackend) {}

  public refresh(): void {
    this._onDidChangeTreeData.fire(null);
  }

  public async performSearch(term: string) {
    const searchResponse = await this.backend.sendSearch({ term });
    this.lastSearches.push({
      term,
      results: searchResponse?.results ?? [],
    });
    this.refresh();
    if (searchResponse?.shouldUpdateAssemblyList) {
      commands.executeCommand("ilspy.refreshAssemblyList");
    }
  }

  public getTreeItem(node: SearchTreeNode): TreeItem {
    if (isPerformedSearchNode(node)) {
      return {
        label: `Search results for "${node.term}"`,
        collapsibleState: TreeItemCollapsibleState.Expanded,
        iconPath: new ThemeIcon("search-view-icon"),
      };
    } else {
      return {
        label: node.displayName,
        description: node.description,
        tooltip: node.description,
        collapsibleState: TreeItemCollapsibleState.None,
        command: {
          command: "ilspy.decompileNode",
          arguments: [node],
          title: "Decompile",
        },
        iconPath: new ThemeIcon(getNodeIcon(node.metadata?.type)),
      };
    }
  }

  public findNode(predicate: (node: SearchTreeNode) => boolean) {
    return (this.getChildren() as Node[]).find(predicate);
  }

  public getChildren(
    node?: SearchTreeNode
  ): SearchTreeNode[] | Thenable<SearchTreeNode[]> {
    if (!node) {
      return [...this.lastSearches].reverse();
    }

    if (isPerformedSearchNode(node)) {
      return node.results;
    }

    return [];
  }

  public getParent?(element: SearchTreeNode): ProviderResult<SearchTreeNode> {
    // Note: This allows relealing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }
}
