/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import {
  TreeDataProvider,
  EventEmitter,
  TreeItem,
  Event,
  TreeItemCollapsibleState,
  ProviderResult,
  window,
  ThemeIcon,
  ExtensionContext,
  commands,
} from "vscode";
import IILSpyBackend from "./IILSpyBackend";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";
import { getAssemblyList, updateAssemblyListIfNeeded } from "./settings";
import { getNodeIcon } from "../icons";

export class DecompiledTreeProvider implements TreeDataProvider<Node> {
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;

  constructor(
    private extensionContext: ExtensionContext,
    private backend: IILSpyBackend
  ) {}

  public refresh(): void {
    this._onDidChangeTreeData.fire(null);
  }

  public async initWithAssemblies(): Promise<boolean> {
    const assemblyPaths = getAssemblyList(this.extensionContext);
    const response = await this.backend.sendInitWithAssemblies({
      assemblyPaths,
    });
    if (response?.loadedAssemblies) {
      this.refresh();
      return true;
    } else {
      window.showWarningMessage(
        `Assemblies could not be restored from last session.`
      );
    }

    return false;
  }

  public async addAssembly(assembly: string): Promise<boolean> {
    const response = await this.backend.sendAddAssembly({
      assemblyPath: assembly,
    });
    if (response?.added && response?.assemblyData) {
      this.refresh();
      return true;
    } else {
      window.showWarningMessage(
        `File '${assembly}' could not be loaded as assembly.`
      );
    }

    return false;
  }

  public async removeAssembly(assembly: string): Promise<boolean> {
    const response = await this.backend.sendRemoveAssembly({
      assemblyPath: assembly,
    });
    if (response?.removed) {
      this.refresh();
    }
    return response?.removed ?? false;
  }

  public async reloadAssembly(assembly: string): Promise<boolean> {
    const response = await this.backend.sendRemoveAssembly({
      assemblyPath: assembly,
    });
    if (response?.removed) {
      const response = await this.backend.sendAddAssembly({
        assemblyPath: assembly,
      });
      if (response?.added && response?.assemblyData) {
        this.refresh();
        return true;
      }
    }
    return false;
  }

  public getTreeItem(node: Node): TreeItem {
    return {
      label: node.displayName,
      tooltip: node.description,
      collapsibleState: node.mayHaveChildren
        ? TreeItemCollapsibleState.Collapsed
        : void 0,
      command: {
        command: "decompileNode",
        arguments: [node],
        title: "Decompile",
      },
      contextValue: getNodeContextValue(node),
      iconPath: new ThemeIcon(getNodeIcon(node.metadata?.type)),
    };
  }

  public async findNode(predicate: (node: Node) => boolean) {
    return ((await this.getChildren()) as Node[]).find(predicate);
  }

  public getChildren(node?: Node): Node[] | Thenable<Node[]> {
    return this.getChildNodes(node);
  }

  public getParent?(node: Node): ProviderResult<Node> {
    // Note: This allows releasing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }

  async getChildNodes(node?: Node): Promise<Node[]> {
    const result = await this.backend.sendGetNodes({
      nodeMetadata: node?.metadata,
    });

    if (!node) {
      if (result?.nodes) {
        updateAssemblyListIfNeeded(
          this.extensionContext,
          result.nodes
            .filter((node) => node.metadata?.type === NodeType.Assembly)
            .map((node) => node.metadata!.assemblyPath)
        );
      }

      setTreeWithNodes(result?.nodes !== undefined && result.nodes?.length > 0);
    }

    return result?.nodes ?? [];
  }
}

function setTreeWithNodes(treeWithNodes: boolean) {
  commands.executeCommand("setContext", "ilspy.treeWithNodes", treeWithNodes);
}

function getNodeContextValue(node: Node) {
  switch (node.metadata?.type) {
    case NodeType.Assembly:
      return "assemblyNode";
    case NodeType.Class:
    case NodeType.Interface:
    case NodeType.Event:
    case NodeType.Method:
    case NodeType.Enum:
    case NodeType.Const:
    case NodeType.Property:
      return "analyzableNode";
    default:
      break;
  }

  return undefined;
}
