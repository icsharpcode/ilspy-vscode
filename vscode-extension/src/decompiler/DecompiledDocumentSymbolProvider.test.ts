import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("vscode", () => {
  class Position {
    constructor(
      public line: number,
      public character: number,
    ) {}
  }

  class Range {
    public start: Position;
    public end: Position;
    constructor(
      startOrLine: Position | number,
      endOrCharacter: Position | number,
      endLine?: number,
      endCharacter?: number,
    ) {
      if (typeof startOrLine === "number") {
        this.start = new Position(startOrLine, endOrCharacter as number);
        this.end = new Position(endLine as number, endCharacter as number);
      } else {
        this.start = startOrLine;
        this.end = endOrCharacter as Position;
      }
    }
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
  LanguageName,
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
  GetNodePositionsParams,
  GetNodePositionsResponse,
} from "../protocol/getNodePositions";
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
    sendGetNodePositions: vi.fn(
      async (_params: GetNodePositionsParams) =>
        null as GetNodePositionsResponse | null,
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
    vi.mocked(backend.sendGetNodePositions).mockResolvedValue({
      positions: [
        {
          symbolToken: 201,
          startLine: 2,
          startCharacter: 4,
          endLine: 4,
          endCharacter: 5,
        },
        {
          symbolToken: 202,
          startLine: 6,
          startCharacter: 4,
          endLine: 8,
          endCharacter: 5,
        },
      ],
      shouldUpdateAssemblyList: false,
    });

    const provider = new DecompiledDocumentSymbolProvider(
      backend,
      () => LanguageName.CSharpLatest,
    );
    const document = createDocument(parentNode);

    const symbols = await provider.provideDocumentSymbols(
      document,
      { isCancellationRequested: false } as CancellationToken,
    );

    expect(backend.sendGetNodes).toHaveBeenCalledWith({
      nodeMetadata: parentNode.metadata,
    });
    expect(backend.sendGetNodePositions).toHaveBeenCalledWith({
      nodeMetadata: parentNode.metadata,
      outputLanguage: LanguageName.CSharpLatest,
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
    expect(symbols[0].range).toEqual(
      new vscode.Range(2, 4, 4, 5),
    );
    expect(symbols[0].selectionRange).toEqual(new vscode.Range(2, 4, 2, 4));
    expect(symbols[1].range).toEqual(
      new vscode.Range(6, 4, 8, 5),
    );
    expect(symbols[1].selectionRange).toEqual(new vscode.Range(6, 4, 6, 4));
  });

  it("falls back to the document start when no positions are available", async () => {
    const backend = createBackend();
    const parentNode = createNode({
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
      metadata: createNodeMetadata({
        name: "DoWork",
        type: NodeType.Method,
        symbolToken: 201,
        parentSymbolToken: 200,
      }),
    });
    vi.mocked(backend.sendGetNodes).mockResolvedValue({
      nodes: [methodNode],
      shouldUpdateAssemblyList: false,
    });
    vi.mocked(backend.sendGetNodePositions).mockResolvedValue({
      positions: [],
      shouldUpdateAssemblyList: false,
    });

    const provider = new DecompiledDocumentSymbolProvider(
      backend,
      () => LanguageName.CSharpLatest,
    );
    const symbols = await provider.provideDocumentSymbols(
      createDocument(parentNode),
      { isCancellationRequested: false } as CancellationToken,
    );

    expect(symbols).toHaveLength(1);
    expect(symbols[0].range.start).toEqual(new vscode.Position(0, 0));
    expect(symbols[0].range.end).toEqual(new vscode.Position(0, 0));
    expect(symbols[0].selectionRange.start).toEqual(new vscode.Position(0, 0));
    expect(symbols[0].selectionRange.end).toEqual(new vscode.Position(0, 0));
  });

  it("shows a single member itself as symbol for member documents", async () => {
    const backend = createBackend();
    const methodNode = createNode({
      displayName: "DoWork() : void",
      metadata: createNodeMetadata({
        name: "DoWork() : void",
        type: NodeType.Method,
        symbolToken: 201,
        parentSymbolToken: 200,
      }),
    });
    vi.mocked(backend.sendGetNodes).mockResolvedValue({
      nodes: [],
      shouldUpdateAssemblyList: false,
    });
    vi.mocked(backend.sendGetNodePositions).mockResolvedValue({
      positions: [
        {
          symbolToken: 201,
          startLine: 0,
          startCharacter: 0,
          endLine: 0,
          endCharacter: 0,
        },
      ],
      shouldUpdateAssemblyList: false,
    });

    const provider = new DecompiledDocumentSymbolProvider(
      backend,
      () => LanguageName.CSharpLatest,
    );
    const symbols = await provider.provideDocumentSymbols(
      createDocument(methodNode, "public void DoWork() { }"),
      { isCancellationRequested: false } as CancellationToken,
    );

    expect(symbols).toHaveLength(1);
    expect(symbols[0]).toEqual(
      expect.objectContaining({
        name: "DoWork() : void",
        kind: vscode.SymbolKind.Method,
      }),
    );
    expect(symbols[0].range.start).toEqual(new vscode.Position(0, 0));
    expect(symbols[0].range.end).toEqual(new vscode.Position(0, 0));
    expect(symbols[0].selectionRange.start).toEqual(new vscode.Position(0, 0));
    expect(symbols[0].selectionRange.end).toEqual(new vscode.Position(0, 0));
  });

  it("shows the namespace itself when its code contains no type declarations", async () => {
    const backend = createBackend();
    const namespaceNode = createNode({
      displayName: "MyNamespace",
      metadata: createNodeMetadata({
        name: "MyNamespace",
        type: NodeType.Namespace,
        symbolToken: 0,
        parentSymbolToken: 0,
      }),
    });
    const typeNode = createNode({
      displayName: "MyType",
      metadata: createNodeMetadata({
        name: "MyType",
        type: NodeType.Class,
        symbolToken: 201,
        parentSymbolToken: 0,
      }),
    });
    vi.mocked(backend.sendGetNodes).mockResolvedValue({
      nodes: [typeNode],
      shouldUpdateAssemblyList: false,
    });
    vi.mocked(backend.sendGetNodePositions).mockResolvedValue({
      positions: [
        {
          symbolToken: 0,
          startLine: 0,
          startCharacter: 0,
          endLine: 0,
          endCharacter: 0,
        },
      ],
      shouldUpdateAssemblyList: false,
    });

    const provider = new DecompiledDocumentSymbolProvider(
      backend,
      () => LanguageName.CSharpLatest,
    );
    const symbols = await provider.provideDocumentSymbols(
      createDocument(namespaceNode, "namespace MyNamespace { }"),
      { isCancellationRequested: false } as CancellationToken,
    );

    expect(symbols).toHaveLength(1);
    expect(symbols[0]).toEqual(
      expect.objectContaining({
        name: "MyNamespace",
        kind: vscode.SymbolKind.Namespace,
      }),
    );
    expect(symbols[0].range.start).toEqual(new vscode.Position(0, 0));
    expect(symbols[0].selectionRange.start).toEqual(new vscode.Position(0, 0));
  });

  it("returns no symbols for non-code ilspy documents", async () => {
    const backend = createBackend();
    const provider = new DecompiledDocumentSymbolProvider(
      backend,
      () => LanguageName.CSharpLatest,
    );
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
    expect(backend.sendGetNodePositions).not.toHaveBeenCalled();
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
