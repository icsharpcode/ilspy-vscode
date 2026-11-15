/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as os from "os";
import * as path from "path";
import {
  CancellationToken,
  commands,
  DataTransfer,
  Event,
  EventEmitter,
  ExtensionContext,
  TreeDataProvider,
  ThemeIcon,
  TreeDragAndDropController,
  TreeItem,
  TreeItemCollapsibleState,
  Uri,
  window,
} from "vscode";
import { getNodeIcon } from "../icons";
import OutputWindowLogger from "../OutputWindowLogger";
import {
  AvailableNodeCommands,
  Node,
  NodeFlags,
  NodeType,
} from "../extension-types";
import IILSpyBackend from "./IILSpyBackend";
import {
  getAssemblyList,
  getAutoLoadDependenciesSetting,
  getGroupAssembliesByFolderSetting,
  getShowCompilerGeneratedSymbolsSetting,
  updateAssemblyListIfNeeded,
} from "./settings";
import {
  ASSEMBLY_FILE_EXTENSIONS,
  createNodeId,
  createNodeTooltip,
  getNodeContextValue,
  hasNodeCommand,
  hasNodeFlag,
  isSameNode,
} from "./utils";
import { executeILSpyCommand } from "../commands/commandUtils";
import {
  AssemblyFolderNode,
  collectFolderEntries,
  groupByFolder,
  isAssemblyFolderNode,
} from "./assemblyFolders";

export class DecompiledTreeProvider
  implements TreeDataProvider<Node>, TreeDragAndDropController<Node>
{
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;

  // Root entries (assemblies, packages) as shown in the tree, and the folder nodes grouping them.
  private rootEntries: Node[] = [];
  private rootEntryFolders = new Map<Node, AssemblyFolderNode>();

  constructor(
    private extensionContext: ExtensionContext,
    private backend: IILSpyBackend,
    private logger: OutputWindowLogger,
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
        `Assemblies could not be restored from last session.`,
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
        `File '${assembly}' could not be loaded as assembly.`,
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

  public async clearAssemblies(): Promise<void> {
    const rootNodes =
      (await this.backend.sendGetNodes({ nodeMetadata: undefined }))?.nodes ??
      [];
    for (const node of rootNodes) {
      if (
        node.metadata &&
        hasNodeCommand(node, AvailableNodeCommands.ManageRootEntries)
      ) {
        await this.backend.sendRemoveAssembly({
          assemblyPath: node.metadata.assemblyPath,
        });
      }
    }
    this.refresh();
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
    if (isAssemblyFolderNode(node)) {
      return {
        id: `assemblyFolder:${node.folderPath}`,
        label: node.displayName,
        tooltip: node.folderPath,
        // Folders with only auto-loaded dependencies would push the user's assemblies out of sight.
        collapsibleState: node.containsLoadedEntries
          ? TreeItemCollapsibleState.Expanded
          : TreeItemCollapsibleState.Collapsed,
        contextValue: "assemblyFolder",
        iconPath: new ThemeIcon("folder"),
      };
    }

    return {
      id: createNodeId(node),
      label: node.displayName,
      tooltip: createNodeTooltip(node),
      collapsibleState: node.mayHaveChildren
        ? TreeItemCollapsibleState.Collapsed
        : void 0,
      command: {
        command: "ilspy.decompileNode",
        arguments: [node],
        title: "Decompile",
      },
      contextValue: getNodeContextValue(node),
      iconPath: getNodeIcon(node.metadata?.type),
      resourceUri: hasNodeFlag(node, NodeFlags.AutoLoaded)
        ? Uri.parse(
            `ilspy-autoloaded://${node.metadata?.assemblyPath}/${
              node.metadata?.name ?? ""
            }`,
          )
        : undefined,
    };
  }

  public async findNode(predicate: (node: Node) => boolean) {
    await this.getChildNodes();
    return this.rootEntries.find(predicate);
  }

  /**
   * Finds the node of this tree, which represents the given node (e.g. a search or analyzer result).
   * Walks down from the root along the path resolved by the backend. If the node itself is not shown
   * in the tree (e.g. it's compiler-generated or located in a compiler-generated type), the closest
   * visible ancestor is returned instead.
   */
  public async findTreeNode(node: Node): Promise<Node | undefined> {
    if (!node.metadata) {
      return undefined;
    }

    const response = await this.backend.sendResolveNodePath({
      nodeMetadata: node.metadata,
    });
    const path = [...(response?.nodePath ?? []), node];

    let closestTreeNode: Node | undefined;
    for (const pathNode of path) {
      // Root entries may be grouped in folder nodes, which are not part of the backend's path.
      const children = closestTreeNode
        ? await this.getChildNodes(closestTreeNode)
        : (await this.getChildNodes(), this.rootEntries);
      const treeNode = children.find((child) => isSameNode(child, pathNode));
      if (!treeNode) {
        break;
      }
      closestTreeNode = treeNode;
    }

    return closestTreeNode;
  }

  public getChildren(node?: Node): Node[] | Thenable<Node[]> {
    return this.getChildNodes(node);
  }

  public async getParent?(node: Node): Promise<Node | undefined> {
    if (isAssemblyFolderNode(node)) {
      return node.parentFolder;
    }
    const rootEntryFolder = this.findRootEntryFolder(node);
    if (rootEntryFolder) {
      return rootEntryFolder;
    }

    const path = await this.backend.sendResolveNodePath({
      nodeMetadata: node.metadata,
    });
    if (path?.nodePath && path.nodePath.length > 0) {
      return path.nodePath[path.nodePath?.length - 1];
    }

    return undefined;
  }

  private findRootEntryFolder(node: Node) {
    for (const [entry, folder] of this.rootEntryFolders) {
      if (isSameNode(entry, node)) {
        return folder;
      }
    }
    return undefined;
  }

  async getChildNodes(node?: Node): Promise<Node[]> {
    if (isAssemblyFolderNode(node)) {
      return node.folderChildren;
    }

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
                !hasNodeFlag(node, NodeFlags.AutoLoaded),
            )
            .map((node) => node.metadata!.assemblyPath),
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
    const nodes =
      result?.nodes?.filter(
        (node) =>
          (showAutoLoadedAssemblies ||
            !hasNodeFlag(node, NodeFlags.AutoLoaded)) &&
          (showCompilerGeneratedSymbols ||
            !hasNodeFlag(node, NodeFlags.CompilerGenerated)),
      ) ?? [];
    return node ? nodes : this.updateRootEntries(nodes);
  }

  private updateRootEntries(rootEntries: Node[]): Node[] {
    this.rootEntries = rootEntries;
    this.rootEntryFolders.clear();
    if (!getGroupAssembliesByFolderSetting()) {
      return rootEntries;
    }

    const groupedNodes = groupByFolder(rootEntries, {
      homeDir: os.homedir(),
    });
    for (const { entry, folder } of collectFolderEntries(groupedNodes)) {
      if (folder) {
        this.rootEntryFolders.set(entry, folder);
      }
    }
    return groupedNodes;
  }

  readonly dropMimeTypes = ["text/uri-list"];
  readonly dragMimeTypes = [];

  async handleDrop(
    target: Node | undefined,
    dataTransfer: DataTransfer,
    token: CancellationToken,
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
          `Dropped assembly file ${filePath}, trying to add to assembly list`,
        );
      } else {
        this.logger.writeLine(`Dropped unsupported file ${filePath}!`);
      }
    });
  }

  private isValidFile(filePath: string): boolean {
    const ext = path.extname(filePath);
    return ASSEMBLY_FILE_EXTENSIONS.includes(
      path.extname(filePath).substring(1),
    );
  }

  private processDroppedFile(target: Node | undefined, filePath: string): void {
    executeILSpyCommand("ilspy.addAssemblyByPath", filePath);
  }
}

function setTreeWithNodes(treeWithNodes: boolean) {
  commands.executeCommand("setContext", "ilspy.treeWithNodes", treeWithNodes);
}
