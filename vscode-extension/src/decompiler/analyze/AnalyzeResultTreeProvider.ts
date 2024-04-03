/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2024 ICSharpCode
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
import { ProductIconMapping } from "../../icons";
import { NodeType } from "../../protocol/NodeType";

interface PerformedAnalyze {
  symbol: string;
  results: Node[];
}

export type AnalyzeTreeNode = Node | PerformedAnalyze;

export class AnalyzeResultTreeProvider
  implements TreeDataProvider<AnalyzeTreeNode>
{
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;
  private lastAnalyzes: PerformedAnalyze[] = [];

  constructor(private backend: IILSpyBackend) {}

  public refresh(): void {
    this._onDidChangeTreeData.fire(null);
  }

  public async analyze(node: Node) {
    this.lastAnalyzes.push({
      symbol: node.displayName,
      results:
        (await this.backend.sendAnalyze({ node: node.metadata }))?.results ??
        [],
    });
    this.refresh();
  }

  public getTreeItem(element: AnalyzeTreeNode): TreeItem {
    if ((element as PerformedAnalyze).symbol) {
      const performedSearch = element as PerformedAnalyze;
      return {
        label: `Analyze "${performedSearch.symbol}"`,
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
        iconPath: new ThemeIcon(
          ProductIconMapping[nodeData.metadata?.type ?? NodeType.Unknown]
        ),
      };
    }
  }

  public findNode(predicate: (node: AnalyzeTreeNode) => boolean) {
    return (this.getChildren() as Node[]).find(predicate);
  }

  public getChildren(
    element?: AnalyzeTreeNode
  ): AnalyzeTreeNode[] | Thenable<AnalyzeTreeNode[]> {
    if (!element) {
      return [...this.lastAnalyzes].reverse();
    }

    if ((element as PerformedAnalyze).symbol) {
      return (element as PerformedAnalyze).results;
    }

    return [];
  }

  public getParent?(element: AnalyzeTreeNode): ProviderResult<AnalyzeTreeNode> {
    // Note: This allows relealing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }
}
