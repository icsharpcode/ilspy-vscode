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
  ExtensionContext,
  commands,
  Uri,
  DataTransfer,
  TreeDragAndDropController,
  CancellationToken,
} from "vscode";
import IILSpyBackend from "./IILSpyBackend";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";
import {
  getAssemblyList,
  getAutoLoadDependenciesSetting,
  getShowCompilerGeneratedSymbolsSetting,
  updateAssemblyListIfNeeded,
} from "./settings";
import { getNodeIcon } from "../icons";
import { NodeFlags } from "../protocol/NodeFlags";
import {
  ASSEMBLY_FILE_EXTENSIONS,
  createNodeTooltip,
  getNodeContextValue,
  hasNodeFlag,
} from "./utils";
import path = require("path");
import OutputWindowLogger from "../OutputWindowLogger";
import { addAssemblyFromFilePath } from "../commands/utils";

export class DecompiledTreeProvider
  implements TreeDataProvider<Node>, TreeDragAndDropController<Node>
{
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;

  constructor(
    private extensionContext: ExtensionContext,
    private backend: IILSpyBackend,
    private logger: OutputWindowLogger
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
    const test = getNodeIcon(node.metadata?.type);

    return {
      label: node.displayName,
      tooltip: createNodeTooltip(node),
      collapsibleState: node.mayHaveChildren
        ? TreeItemCollapsibleState.Collapsed
        : void 0,
      command: node.metadata?.isDecompilable
        ? {
            command: "ilspy.decompileNode",
            arguments: [node],
            title: "Decompile",
          }
        : undefined,
      contextValue: getNodeContextValue(node),
      iconPath: getNodeIcon(node.metadata?.type),
      resourceUri: hasNodeFlag(node, NodeFlags.AutoLoaded)
        ? Uri.parse(
            `ilspy-autoloaded://${node.metadata?.assemblyPath}/${
              node.metadata?.name ?? ""
            }`
          )
        : undefined,
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
            .filter(
              (node) =>
                (node.metadata?.type === NodeType.Assembly ||
                  node.metadata?.type === NodeType.NuGetPackage) &&
                !hasNodeFlag(node, NodeFlags.AutoLoaded)
            )
            .map((node) => node.metadata!.assemblyPath)
        );
      }

      setTreeWithNodes(result?.nodes !== undefined && result.nodes?.length > 0);
    }

    if (result?.shouldUpdateAssemblyList) {
      this.refresh();
    }

    const showAutoLoadedAssemblies = getAutoLoadDependenciesSetting();
    const showCompilerGeneratedSymbols =
      getShowCompilerGeneratedSymbolsSetting();
    return (
      result?.nodes?.filter(
        (node) =>
          (showAutoLoadedAssemblies ||
            !hasNodeFlag(node, NodeFlags.AutoLoaded)) &&
          (showCompilerGeneratedSymbols ||
            !hasNodeFlag(node, NodeFlags.CompilerGenerated))
      ) ?? []
    );
  }

  readonly dropMimeTypes = ["text/uri-list"];
  readonly dragMimeTypes = [];

  async handleDrop(
    target: Node | undefined,
    dataTransfer: DataTransfer,
    token: CancellationToken
  ): Promise<void> {
    const uris =
      (await dataTransfer.get("text/uri-list")?.asString())?.split("\r\n") ??
      [];
    uris.forEach((uri) => {
      const fileUri = Uri.parse(uri);
      const filePath = fileUri.fsPath;
      if (this.isValidFile(fileUri.fsPath)) {
        this.processDroppedFile(target, filePath);
        this.logger.writeLine(
          `Dropped assembly file ${filePath}, trying to add to assembly list`
        );
      } else {
        this.logger.writeLine(`Dropped unsupported file ${filePath}!`);
      }
    });
  }

  private isValidFile(filePath: string): boolean {
    const ext = path.extname(filePath);
    return ASSEMBLY_FILE_EXTENSIONS.includes(
      path.extname(filePath).substring(1)
    );
  }

  private processDroppedFile(target: Node | undefined, filePath: string): void {
    commands.executeCommand("ilspy.addAssemblyByPath", filePath);
  }
}

function setTreeWithNodes(treeWithNodes: boolean) {
  commands.executeCommand("setContext", "ilspy.treeWithNodes", treeWithNodes);
}


