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
} from "vscode";
import { DecompiledCode } from "../protocol/DecompileResponse";
import IILSpyBackend from "./IILSpyBackend";
import AssemblyData from "../protocol/AssemblyData";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";
import NodeMetadata from "../protocol/NodeMetadata";
import { ProductIconMapping } from "../icons";

export class DecompiledTreeProvider implements TreeDataProvider<Node> {
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;

  constructor(private backend: IILSpyBackend) {}

  public refresh(): void {
    this._onDidChangeTreeData.fire(null);
  }

  public async addAssembly(assembly: string): Promise<boolean> {
    const response = await this.backend.sendAddAssembly({
      assemblyPath: assembly,
    });
    if (response?.added && response?.assemblyData) {
      this.backend.assemblies.set(assembly, response.assemblyData);
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
      this.backend.assemblies.delete(assembly);
      this.refresh();
    }
    return response?.removed ?? false;
  }

  public async reloadAssembly(assembly: string): Promise<boolean> {
    const response = await this.backend.sendRemoveAssembly({
      assemblyPath: assembly,
    });
    if (response?.removed) {
      this.backend.assemblies.delete(assembly);

      const response = await this.backend.sendAddAssembly({
        assemblyPath: assembly,
      });
      if (response?.added && response?.assemblyData) {
        this.backend.assemblies.set(assembly, response.assemblyData);
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
      contextValue:
        node.metadata?.type === NodeType.Assembly ? "assemblyNode" : void 0,
      iconPath: new ThemeIcon(
        ProductIconMapping[node.metadata?.type ?? NodeType.Unknown]
      ),
    };
  }

  public findNode(predicate: (node: Node) => boolean) {
    return (this.getChildren() as Node[]).find(predicate);
  }

  public getChildren(node?: Node): Node[] | Thenable<Node[]> {
    if (this.backend.assemblies.size <= 0) {
      return [];
    }

    // Nothing yet so add assembly nodes
    if (!node) {
      let result = [] as Node[];
      for (let assemblyData of this.backend.assemblies.values()) {
        result.push({
          displayName: getAssemblyNodeText(assemblyData),
          description: assemblyData.filePath,
          mayHaveChildren: true,
          metadata: {
            assemblyPath: assemblyData.filePath,
            type: NodeType.Assembly,
            name: assemblyData.name,
          } as NodeMetadata,
        } as Node);
      }

      return result;
    } else {
      return this.getChildNodes(node);
    }
  }

  public getParent?(node: Node): ProviderResult<Node> {
    // Note: This allows releasing of assembly nodes in TreeView, which are placed in root. It won't work for other nodes.
    return undefined;
  }

  async getChildNodes(node: Node): Promise<Node[]> {
    if (!node.metadata) {
      return [];
    }

    const result = await this.backend.sendGetNodes({
      nodeMetadata: node.metadata!,
    });
    return result?.nodes ?? [];
  }

  public async getCode(node: Node): Promise<DecompiledCode | undefined> {
    if (!node.metadata) {
      return undefined;
    }

    const result = await this.backend.sendDecompileNode({
      nodeMetadata: node.metadata!,
    });
    return result?.decompiledCode;
  }
}

function getAssemblyNodeText(assemblyData: AssemblyData) {
  const text = assemblyData.name;
  const additionalData = [assemblyData.version, assemblyData.targetFramework]
    .filter((d) => d)
    .join(", ");
  return `${text}${additionalData ? ` (${additionalData})` : ""}`;
}

