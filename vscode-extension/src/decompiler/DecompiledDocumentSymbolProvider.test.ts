import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("vscode", () => {
  class Position {
    constructor(
      public line: number,
      public character: number,
    ) {}
  }

  class Range {
    constructor(
      public start: Position,
      public end: Position,
    ) {}
  }

  class DocumentSymbol {
    constructor(
      public name: string,
      public detail: string,
      public kind: number,
      public range: Range,
      public selectionRange: Range,
      public children: DocumentSymbol[] = [],
    ) {}
  }

  return {
    Position,
    Range,
    DocumentSymbol,
    SymbolKind: {
      Module: 1,
      Namespace: 2,
      Class: 3,
      Interface: 4,
      Struct: 5,
      Enum: 6,
      Function: 7,
      Event: 8,
      Field: 9,
      Method: 10,
      Constant: 11,
      Property: 12,
      Object: 13,
      Package: 14,
      File: 15,
    },
    commands: {
      executeCommand: vi.fn(),
    },
    languages: {
      registerDocumentSymbolProvider: vi.fn(),
    },
    Uri: {
      file: (fsPath: string) => {
        const uri = {
          fsPath,
          scheme: "file",
          query: "",
          with(opts: Record<string, unknown>) {
            return Object.assign(uri, opts);
          },
        };
        return uri;
      },
    },
  };
});

import type { CancellationToken, TextDocument } from "vscode";
import * as vscode from "vscode";
import type IILSpyBackend from "./IILSpyBackend";
import {
  AvailableNodeCommands,
  Node,
  NodeMetadata,
  NodeType,
} from "../extension-types";
import {
  DecompiledDocumentSymbolProvider,
  registerDecompiledDocumentSymbolProvider,
} from "./DecompiledDocumentSymbolProvider";
import { ILSPY_URI_SCHEME, nodeDataToUri } from "./nodeUri";
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
import type {
  ResolveNodePathParams,
  ResolveNodePathResponse,
} from "../protocol/resolveNodePath";

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
    sendResolveNodePath: vi.fn(
      async (_params: ResolveNodePathParams) =>
        null as ResolveNodePathResponse | null,
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

function createDocument(node: Node, text = "class MyType {}"): TextDocument {
  return {
    uri: nodeDataToUri(node),
    positionAt: (offset: number) =>
      new vscode.Position(0, Math.max(0, Math.min(offset, text.length))),
  } as TextDocument;
}

describe("DecompiledDocumentSymbolProvider", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("creates document symbols from the decompiled node children", async () => {
    const backend = createBackend();
    const parentNode = createNode({
      displayName: "ParentType",
      metadata: createNodeMetadata({
        name: "Namespace.ParentType",
        type: NodeType.Class,
        symbolToken: 200,
        parentSymbolToken: 100,
      }),
    });
    const methodNode = createNode({
      displayName: "DoWork",
      description: "void DoWork()",
      mayHaveChildren: false,
      metadata: createNodeMetadata({
        name: "DoWork",
        type: NodeType.Method,
        symbolToken: 201,
        parentSymbolToken: 200,
      }),
    });
    const propertyNode = createNode({
      displayName: "Value",
      description: "int Value",
      mayHaveChildren: false,
      metadata: createNodeMetadata({
        name: "Value",
        type: NodeType.Property,
        symbolToken: 202,
        parentSymbolToken: 200,
      }),
    });
    const resourceNode = createNode({
      displayName: "readme.txt",
      description: "resource",
      mayHaveChildren: false,
      metadata: createNodeMetadata({
        name: "readme.txt",
        type: NodeType.Resource,
        symbolToken: 203,
        parentSymbolToken: 200,
        availableCommands: AvailableNodeCommands.None,
      }),
    });
    vi.mocked(backend.sendGetNodes).mockResolvedValue({
      nodes: [methodNode, propertyNode, resourceNode],
      shouldUpdateAssemblyList: false,
    });

    const provider = new DecompiledDocumentSymbolProvider(backend);
    const document = createDocument(parentNode);

    const symbols = await provider.provideDocumentSymbols(
      document,
      { isCancellationRequested: false } as CancellationToken,
    );

    expect(backend.sendGetNodes).toHaveBeenCalledWith({
      nodeMetadata: parentNode.metadata,
    });
    expect(symbols).toHaveLength(2);
    expect(symbols).toEqual([
      expect.objectContaining({
        name: "DoWork",
        detail: "void DoWork()",
        kind: vscode.SymbolKind.Method,
      }),
      expect.objectContaining({
        name: "Value",
        detail: "int Value",
        kind: vscode.SymbolKind.Property,
      }),
    ]);
    for (const symbol of symbols) {
      expect(symbol.range.start).toEqual(new vscode.Position(0, 0));
      expect(symbol.range.end).toEqual(new vscode.Position(0, 0));
      expect(symbol.selectionRange.start).toEqual(new vscode.Position(0, 0));
      expect(symbol.selectionRange.end).toEqual(new vscode.Position(0, 0));
    }
  });

  it("returns no symbols for non-code ilspy documents", async () => {
    const backend = createBackend();
    const provider = new DecompiledDocumentSymbolProvider(backend);
    const document = createDocument(
      createNode({
        displayName: "Resources",
        metadata: createNodeMetadata({
          name: "Resources",
          type: NodeType.Resource,
          symbolToken: 300,
          parentSymbolToken: 0,
          availableCommands: AvailableNodeCommands.None,
        }),
      }),
    );

    const symbols = await provider.provideDocumentSymbols(
      document,
      { isCancellationRequested: false } as CancellationToken,
    );

    expect(symbols).toEqual([]);
    expect(backend.sendGetNodes).not.toHaveBeenCalled();
  });

  it("registers against the ilspy scheme", () => {
    const backend = createBackend();

    registerDecompiledDocumentSymbolProvider(backend);

    expect(vscode.languages.registerDocumentSymbolProvider).toHaveBeenCalledWith(
      { scheme: ILSPY_URI_SCHEME },
      expect.any(DecompiledDocumentSymbolProvider),
    );
  });
});
