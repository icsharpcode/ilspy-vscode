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
import { NodeType } from "../../protocol/NodeType";
import { getNodeIcon } from "../../icons";
import { getTreeNodeCollapsibleState } from "../utils";

export interface PerformedAnalyze {
  symbol: string;
  results: Node[];
}

export type AnalyzeTreeNode = Node | PerformedAnalyze;

export function isPerformedAnalyzeNode(
  node: AnalyzeTreeNode
): node is PerformedAnalyze {
  return "symbol" in node && "results" in node;
}

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
        (await this.backend.sendAnalyze({ nodeMetadata: node.metadata }))
          ?.results ?? [],
    });
    this.refresh();
  }

  public getFirstNode() {
    if (this.lastAnalyzes.length > 0) {
      return this.lastAnalyzes[this.lastAnalyzes.length - 1];
    }

    return undefined;
  }

  public getTreeItem(node: AnalyzeTreeNode): TreeItem {
    if (isPerformedAnalyzeNode(node)) {
      return {
        label: `Analyze "${node.symbol}"`,
        collapsibleState: TreeItemCollapsibleState.Expanded,
        iconPath: new ThemeIcon("search-view-icon"),
      };
    } else if (node.metadata?.type === NodeType.Analyzer) {
      return {
        label: node.displayName,
        collapsibleState: getTreeNodeCollapsibleState(node),
        iconPath: new ThemeIcon(getNodeIcon(node.metadata?.type)),
      };
    } else {
      return {
        label: node.displayName,
        description: node.description,
        tooltip: node.description,
        collapsibleState: getTreeNodeCollapsibleState(node),
        command: {
          command: "ilspy.decompileNode",
          arguments: [node],
          title: "Decompile",
        },
        iconPath: new ThemeIcon(getNodeIcon(node.metadata?.type)),
      };
    }
  }

  public findNode(predicate: (node: AnalyzeTreeNode) => boolean) {
    return (this.getChildren() as Node[]).find(predicate);
  }

  public getChildren(
    node?: AnalyzeTreeNode
  ): AnalyzeTreeNode[] | Thenable<AnalyzeTreeNode[]> {
    return this.getChildNodes(node);
  }

  async getChildNodes(node?: AnalyzeTreeNode): Promise<AnalyzeTreeNode[]> {
    if (!node) {
      return [...this.lastAnalyzes].reverse();
    }

    if (isPerformedAnalyzeNode(node)) {
      return node.results;
    }

    if (node.metadata?.type !== NodeType.Analyzer) {
      return [];
    }

    const result = await this.backend.sendGetNodes({
      nodeMetadata: node?.metadata,
    });

    return result?.nodes ?? [];
  }

  public getParent?(element: AnalyzeTreeNode): ProviderResult<AnalyzeTreeNode> {
    // Note: This allows relealing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }
}
