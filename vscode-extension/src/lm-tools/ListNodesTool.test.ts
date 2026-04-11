import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("vscode", () => {
  class LanguageModelTextPart {
    constructor(public value: string) {}
  }

  class LanguageModelToolResult {
    constructor(public content: LanguageModelTextPart[]) {}
  }

  class MarkdownString {
    constructor(public value: string) {}
  }

  return {
    LanguageModelTextPart,
    LanguageModelToolResult,
    MarkdownString,
    commands: {
      executeCommand: vi.fn(),
    },
  };
});

import type { CancellationToken } from "vscode";
import type IILSpyBackend from "../decompiler/IILSpyBackend";
import { ListNodesTool } from "./ListNodesTool";
import { AvailableNodeCommands } from "../protocol/AvailableNodeCommands";
import { NodeType } from "../protocol/NodeType";
import type Node from "../protocol/Node";
import type NodeMetadata from "../protocol/NodeMetadata";
import type {
  AddAssemblyParams,
  AddAssemblyResponse,
} from "../protocol/addAssembly";
import type { AnalyzeParams, AnalyzeResponse } from "../protocol/analyze";
import type DecompileResponse from "../protocol/DecompileResponse";
import type { DecompileNodeParams } from "../protocol/decompileNode";
import type ExportNodeResponse from "../protocol/exportNode";
import type { ExportNodeParams } from "../protocol/exportNode";
import type { GetNodesParams, GetNodesResponse } from "../protocol/getNodes";
import type {
  InitWithAssembliesParams,
  InitWithAssembliesResponse,
} from "../protocol/initWithAssemblies";
import type {
  RemoveAssemblyParams,
  RemoveAssemblyResponse,
} from "../protocol/removeAssembly";
import type { SearchParams, SearchResponse } from "../protocol/search";

function createBackend(): IILSpyBackend {
  return {
    sendInitWithAssemblies: vi.fn(
      async (_params: InitWithAssembliesParams) =>
        null as InitWithAssembliesResponse | null,
    ),
    sendAddAssembly: vi.fn(
      async (_params: AddAssemblyParams) => null as AddAssemblyResponse | null,
    ),
    sendRemoveAssembly: vi.fn(
      async (_params: RemoveAssemblyParams) =>
        null as RemoveAssemblyResponse | null,
    ),
    sendDecompileNode: vi.fn(
      async (_params: DecompileNodeParams) => null as DecompileResponse | null,
    ),
    sendGetNodes: vi.fn(
      async (_params: GetNodesParams) => null as GetNodesResponse | null,
    ),
    sendSearch: vi.fn(
      async (_params: SearchParams) => null as SearchResponse | null,
    ),
    sendAnalyze: vi.fn(
      async (_params: AnalyzeParams) => null as AnalyzeResponse | null,
    ),
    sendExportAssembly: vi.fn(
      async (_params: ExportNodeParams, _token?: CancellationToken) =>
        null as ExportNodeResponse | null,
    ),
  };
}

function createNodeMetadata(overrides?: Partial<NodeMetadata>): NodeMetadata {
  return {
    assemblyPath: "/tmp/MyAssembly.dll",
    type: NodeType.Class,
    name: "MyType",
    symbolToken: 123,
    parentSymbolToken: 45,
    availableCommands: AvailableNodeCommands.Decompile,
    ...overrides,
  };
}

function createNode(overrides?: Partial<Node>): Node {
  return {
    displayName: "MyType",
    description: "class",
    mayHaveChildren: true,
    modifiers: 0,
    flags: 0,
    metadata: createNodeMetadata(),
    ...overrides,
  };
}

function getJsonResult(result: { content: unknown[] }) {
  const [part] = result.content;
  if (
    typeof part !== "object" ||
    part === null ||
    !("value" in part) ||
    typeof part.value !== "string"
  ) {
    throw new Error("Expected the tool result to contain a text part.");
  }

  return JSON.parse(part.value);
}

describe("ListNodesTool", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("lists root nodes when no target is provided", async () => {
    const backend = createBackend();
    const rootNode = createNode({
      displayName: "MyAssembly",
      description: "assembly",
      metadata: createNodeMetadata({
        type: NodeType.Assembly,
        name: "MyAssembly",
      }),
    });
    vi.mocked(backend.sendGetNodes).mockResolvedValue({
      nodes: [rootNode],
      shouldUpdateAssemblyList: false,
    });

    const tool = new ListNodesTool(backend);
    const result = await tool.invoke(
      { input: {}, toolInvocationToken: undefined },
      {} as CancellationToken,
    );

    expect(backend.sendGetNodes).toHaveBeenCalledWith({});
    expect(getJsonResult(result)).toEqual({
      scope: "root",
      nodes: [
        expect.objectContaining({
          displayName: "MyAssembly",
          mayHaveChildren: true,
          type: "Assembly",
        }),
      ],
    });
  });

  it("uses exact nodeMetadata to list child nodes for any tree entry", async () => {
    const backend = createBackend();
    const childNode = createNode({
      displayName: "lib",
      description: "package folder",
      mayHaveChildren: true,
      metadata: createNodeMetadata({
        type: NodeType.PackageFolder,
        name: "lib",
        symbolToken: 0,
        parentSymbolToken: 0,
        availableCommands: AvailableNodeCommands.None,
      }),
    });
    const targetMetadata = createNodeMetadata({
      type: NodeType.ReferencesRoot,
      name: "References",
      symbolToken: 0,
      parentSymbolToken: 0,
      availableCommands: AvailableNodeCommands.None,
    });
    vi.mocked(backend.sendGetNodes).mockResolvedValue({
      nodes: [childNode],
      shouldUpdateAssemblyList: false,
    });

    const tool = new ListNodesTool(backend);
    const result = await tool.invoke(
      {
        input: {
          nodeMetadata: targetMetadata,
          symbol: "IgnoredSymbol",
          assemblyName: "IgnoredAssembly",
        },
        toolInvocationToken: undefined,
      },
      {} as CancellationToken,
    );

    expect(backend.sendGetNodes).toHaveBeenCalledWith({
      nodeMetadata: targetMetadata,
    });
    expect(backend.sendSearch).not.toHaveBeenCalled();
    expect(getJsonResult(result)).toEqual({
      target: expect.objectContaining({
        name: "References",
        type: "ReferencesRoot",
        nodeMetadata: targetMetadata,
      }),
      nodes: [
        expect.objectContaining({
          displayName: "lib",
          mayHaveChildren: true,
          type: "PackageFolder",
        }),
      ],
    });
  });
});
