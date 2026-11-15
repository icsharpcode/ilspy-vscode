import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const settings = vi.hoisted(() => ({ groupAssembliesByFolder: false }));

vi.mock("vscode", () => {
  class EventEmitter {
    event = () => {};
    fire = vi.fn();
  }
  class ThemeIcon {
    constructor(public id: string) {}
  }
  return {
    EventEmitter,
    ThemeIcon,
    TreeItemCollapsibleState: { None: 0, Collapsed: 1, Expanded: 2 },
    commands: { executeCommand: vi.fn() },
    window: { showWarningMessage: vi.fn() },
    // Boolean settings off by default: no auto-loaded assemblies, no compiler-generated symbols,
    // no grouping by folder.
    workspace: {
      getConfiguration: () => ({
        get: (key: string) =>
          key === "groupAssembliesByFolder"
            ? settings.groupAssembliesByFolder
            : false,
      }),
    },
    Uri: { parse: (p: string) => ({ path: p }) },
  };
});

import type { ExtensionContext } from "vscode";
import type IILSpyBackend from "./IILSpyBackend";
import { DecompiledTreeProvider } from "./DecompiledTreeProvider";
import {
  AssemblyFolderNode,
  groupByFolder,
  isAssemblyFolderNode,
} from "./assemblyFolders";
import { Node, NodeFlags, NodeType } from "../extension-types";
import type OutputWindowLogger from "../OutputWindowLogger";

function node(
  type: NodeType,
  name: string,
  symbolToken = 0,
  parentSymbolToken = 0,
  extra: Partial<Node> = {},
): Node {
  return {
    metadata: {
      assemblyPath: "/a.dll",
      type,
      name,
      symbolToken,
      parentSymbolToken,
      availableCommands: 0,
    },
    displayName: name,
    description: "",
    mayHaveChildren: true,
    symbolModifiers: 0,
    flags: NodeFlags.None,
    ...extra,
  };
}

const assembly = node(NodeType.Assembly, "a.dll");
const namespace = node(NodeType.Namespace, "N");
const outerType = node(NodeType.Class, "Outer", 10);
const stateMachine = node(NodeType.Class, "<Run>d__1", 11, 10, {
  flags: NodeFlags.CompilerGenerated,
});
const runMethod = node(NodeType.Method, "Run() : Task", 20, 10);
const moveNext = node(NodeType.Method, "MoveNext() : void", 21, 11);

const children = new Map<NodeType | undefined, Node[]>([
  [undefined, [assembly]],
  [NodeType.Assembly, [namespace]],
  [NodeType.Namespace, [outerType]],
]);

function createProvider(nodePath: Node[]) {
  const backend = {
    sendResolveNodePath: vi.fn(async () => ({
      nodePath,
      shouldUpdateAssemblyList: false,
    })),
    sendGetNodes: vi.fn(async ({ nodeMetadata }) => {
      if (nodeMetadata?.symbolToken === 10) {
        return { nodes: [stateMachine, runMethod] };
      }
      if (nodeMetadata?.symbolToken === 11) {
        return { nodes: [moveNext] };
      }
      return { nodes: children.get(nodeMetadata?.type) ?? [] };
    }),
  } as unknown as IILSpyBackend;
  const context = {
    workspaceState: { get: () => [], update: vi.fn() },
  } as unknown as ExtensionContext;
  return new DecompiledTreeProvider(context, backend, {} as OutputWindowLogger);
}

describe("DecompiledTreeProvider.findTreeNode", () => {
  it("finds the tree's own node for a search result", async () => {
    const provider = createProvider([assembly, namespace, outerType]);
    // Search results have a different display name than tree nodes.
    const searchResult = { ...runMethod, displayName: "Outer.Run() : Task" };

    const treeNode = await provider.findTreeNode(searchResult);

    expect(treeNode).toBe(runMethod);
  });

  it("finds namespaces by name", async () => {
    const provider = createProvider([assembly]);
    const searchResult = { ...namespace, displayName: "N (in a.dll)" };

    expect(await provider.findTreeNode(searchResult)).toBe(namespace);
  });

  it("falls back to the closest visible ancestor of hidden nodes", async () => {
    const provider = createProvider([
      assembly,
      namespace,
      outerType,
      stateMachine,
    ]);

    expect(await provider.findTreeNode({ ...moveNext })).toBe(outerType);
  });
});

describe("DecompiledTreeProvider with assemblies grouped by folder", () => {
  beforeEach(() => {
    settings.groupAssembliesByFolder = true;
  });
  afterEach(() => {
    settings.groupAssembliesByFolder = false;
  });

  it("shows root entries in expanded folder nodes", async () => {
    const provider = createProvider([]);

    const [folder] = (await provider.getChildren()) as Node[];

    expect(isAssemblyFolderNode(folder)).toBe(true);
    expect((folder as AssemblyFolderNode).folderPath).toBe("/");
    expect(await provider.getChildren(folder)).toEqual([assembly]);
    expect(provider.getTreeItem(folder)).toMatchObject({
      id: "assemblyFolder:/",
      collapsibleState: 2,
      contextValue: "assemblyFolder",
    });
  });

  it("returns folder nodes as parents of root entries", async () => {
    const provider = createProvider([]);
    const [folder] = (await provider.getChildren()) as Node[];

    expect(await provider.getParent!({ ...assembly })).toBe(folder);
    expect(await provider.getParent!(folder)).toBeUndefined();
  });

  it("finds root entries and search results inside folders", async () => {
    const provider = createProvider([assembly, namespace, outerType]);
    const searchResult = { ...runMethod, displayName: "Outer.Run() : Task" };

    expect(
      await provider.findNode((n) => n.metadata?.type === NodeType.Assembly),
    ).toBe(assembly);
    expect(await provider.findTreeNode(searchResult)).toBe(runMethod);
  });

  it("collapses folders containing only auto-loaded dependencies", () => {
    const provider = createProvider([]);
    const inFolder = (n: Node, assemblyPath: string): Node => ({
      ...n,
      metadata: { ...n.metadata!, assemblyPath },
    });
    const loaded = inFolder(assembly, "/app/a.dll");
    const dependency = inFolder(
      node(NodeType.Assembly, "System.Runtime.dll", 0, 0, {
        flags: NodeFlags.AutoLoaded,
      }),
      "/usr/share/dotnet/System.Runtime.dll",
    );
    const [loadedFolder, dependencyFolder] = groupByFolder([
      loaded,
      dependency,
    ]) as AssemblyFolderNode[];

    expect(loadedFolder.folderPath).toBe("/app");
    expect(dependencyFolder.folderPath).toBe("/usr/share/dotnet");
    expect(provider.getTreeItem(loadedFolder).collapsibleState).toBe(2);
    expect(provider.getTreeItem(dependencyFolder).collapsibleState).toBe(1);
  });
});
