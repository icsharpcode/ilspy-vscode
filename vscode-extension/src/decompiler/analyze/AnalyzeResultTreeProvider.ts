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
  commands,
} from "vscode";
import IILSpyBackend from "../IILSpyBackend";
import {
  AvailableNodeCommands,
  Node,
  NodeFlags,
  NodeType,
} from "../../extension-types";
import { getNodeIcon } from "../../icons";
import {
  createNodeId,
  createNodeTooltip,
  getNodeContextValue,
  getTreeNodeCollapsibleState,
  hasNodeCommand,
  hasNodeFlag,
} from "../utils";
import { getShowCompilerGeneratedSymbolsSetting } from "../settings";
import { executeILSpyCommand } from "../../commands/commandUtils";

export interface PerformedAnalyze {
  symbol: string;
  results: AnalyzeResultNode[];
}

type AnalyzeResultNode = Node & {
  treeItemId: string;
};

export type AnalyzeTreeNode = AnalyzeResultNode | PerformedAnalyze;

export function isPerformedAnalyzeNode(
  node: AnalyzeTreeNode
): node is PerformedAnalyze {
  return "symbol" in node && "results" in node;
}

function attachAnalyzeIndex(
  nodes: Node[] | undefined,
  parentTreeItemId: string
): AnalyzeResultNode[] {
  return (
    nodes?.map((node, index) => {
      const nodeId = createNodeId(node);
      const subType = node.metadata?.subType;
      const identitySuffix =
        [nodeId, subType].filter((value) => value !== undefined).join("/") ||
        index.toString();
      return {
        ...node,
        treeItemId: `${parentTreeItemId}/${identitySuffix}`,
      };
    }) ?? []
  );
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
    const analyzeResponse = await this.backend.sendAnalyze({
      nodeMetadata: node.metadata,
    });
    const newAnalyzeIndex = this.lastAnalyzes.length;
    this.lastAnalyzes.push({
      symbol: node.displayName,
      results: attachAnalyzeIndex(
        analyzeResponse?.results,
        newAnalyzeIndex.toString()
      ),
    });
    this.refresh();
    if (analyzeResponse?.shouldUpdateAssemblyList) {
      await executeILSpyCommand("ilspy.refreshAssemblyList");
    }
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
        id: node.treeItemId,
        label: node.displayName,
        collapsibleState: getTreeNodeCollapsibleState(node),
        iconPath: getNodeIcon(node.metadata?.type),
      };
    } else {
      return {
        id: node.treeItemId,
        label: node.displayName,
        description: node.description,
        tooltip: createNodeTooltip(node),
        collapsibleState: getTreeNodeCollapsibleState(node),
        command: {
          command: "ilspy.decompileNode",
          arguments: [node, true],
          title: "Decompile",
        },
        contextValue: getNodeContextValue(node),
        iconPath: getNodeIcon(node.metadata?.type),
      };
    }
  }

  public findNode(predicate: (node: AnalyzeTreeNode) => boolean) {
    return (this.getChildren() as AnalyzeTreeNode[]).find(predicate);
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

    const showCompilerGeneratedSymbols =
      getShowCompilerGeneratedSymbolsSetting();

    if (isPerformedAnalyzeNode(node)) {
      return node.results.filter(
        (node) =>
          showCompilerGeneratedSymbols ||
          !hasNodeFlag(node, NodeFlags.CompilerGenerated)
      );
    }

    if (node.metadata?.type !== NodeType.Analyzer) {
      return [];
    }

    const result = await this.backend.sendGetNodes({
      nodeMetadata: node?.metadata,
    });

    return attachAnalyzeIndex(result?.nodes, node.treeItemId).filter(
      (node) =>
        showCompilerGeneratedSymbols ||
        !hasNodeFlag(node, NodeFlags.CompilerGenerated)
    );
  }

  public getParent?(element: AnalyzeTreeNode): ProviderResult<AnalyzeTreeNode> {
    // Note: This allows relealing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }
}
