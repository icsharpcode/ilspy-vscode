import { describe, expect, it, vi } from "vitest";

vi.mock("vscode", () => {
  type MockUri = {
    fsPath: string;
    scheme?: string;
    query?: string;
    with: (opts: Partial<MockUri>) => MockUri;
  };
  class Range {
    constructor(
      public startLine: number,
      public startCharacter: number,
      public endLine: number,
      public endCharacter: number,
    ) {}
  }
  class Location {
    constructor(
      public uri: MockUri,
      public range: Range,
    ) {}
  }
  return {
    Range,
    Location,
    Uri: {
      file: (fsPath: string): MockUri => {
        const uri: MockUri = {
          fsPath,
          with(opts: Partial<MockUri>) {
            return Object.assign(uri, opts);
          },
        };
        return uri;
      },
    },
    commands: { executeCommand: vi.fn() },
  };
});

import type * as vscode from "vscode";
import type IILSpyBackend from "./IILSpyBackend";
import type { DecompilerTextDocumentContentProvider } from "./DecompilerTextDocumentContentProvider";
import { SymbolNavigationProvider } from "./SymbolNavigationProvider";
import { nodeDataToUri } from "./nodeUri";
import { LanguageName, Node, NodeType } from "../extension-types";
import { CodeLocationsResponse } from "../protocol/symbolNavigation";

function node(name: string, symbolToken: number): Node {
  return {
    metadata: {
      assemblyPath: "/app/App.dll",
      type: NodeType.Method,
      name,
      symbolToken,
      parentSymbolToken: 2,
      availableCommands: 1,
    },
    displayName: name,
    description: "",
    mayHaveChildren: false,
    modifiers: 0,
    flags: 0,
  };
}

const token = { isCancellationRequested: false } as vscode.CancellationToken;

function createProvider(
  sendFindDefinitions: (...args: unknown[]) => Promise<CodeLocationsResponse>,
) {
  const backend = {
    sendFindDefinitions: vi.fn(sendFindDefinitions),
    sendFindImplementations: vi.fn(sendFindDefinitions),
  };
  const contentProvider = {
    getDocumentOutputLanguage: () => LanguageName.IL,
    initDocumentOutputLanguage: vi.fn(),
  };
  const provider = new SymbolNavigationProvider(
    backend as unknown as IILSpyBackend,
    contentProvider as unknown as DecompilerTextDocumentContentProvider,
  );
  const document = {
    uri: nodeDataToUri(node("Caller()", 10)),
  } as vscode.TextDocument;
  return { provider, backend, contentProvider, document };
}

describe("SymbolNavigationProvider", () => {
  it("sends the document node, its output language and the position", async () => {
    const { provider, backend, document } = createProvider(async () => ({
      locations: [],
      shouldUpdateAssemblyList: false,
    }));

    await provider.provideDefinition(
      document,
      { line: 3, character: 7 } as vscode.Position,
      token,
    );

    expect(backend.sendFindDefinitions).toHaveBeenCalledWith(
      {
        nodeMetadata: expect.objectContaining({
          name: "Caller()",
          symbolToken: 10,
        }),
        outputLanguage: LanguageName.IL,
        line: 3,
        column: 7,
      },
      token,
    );
  });

  it("maps locations to the requesting document or the target node's document", async () => {
    const target = node("Callee()", 11);
    const { provider, contentProvider, document } = createProvider(
      async () => ({
        locations: [
          { line: 1, column: 2, length: 3 },
          { node: target, line: 4, column: 5, length: 6 },
        ],
        shouldUpdateAssemblyList: false,
      }),
    );

    const locations = (await provider.provideImplementation(
      document,
      { line: 0, character: 0 } as vscode.Position,
      token,
    )) as unknown as {
      uri: { fsPath: string; query: string };
      range: object;
    }[];

    expect(locations).toHaveLength(2);
    expect(locations[0].uri).toBe(document.uri);
    expect(locations[0].range).toEqual({
      startLine: 1,
      startCharacter: 2,
      endLine: 1,
      endCharacter: 5,
    });
    expect(locations[1].uri.fsPath).toBe(nodeDataToUri(target).fsPath);
    expect(locations[1].uri.query).toBe(nodeDataToUri(target).query);
    expect(locations[1].range).toEqual({
      startLine: 4,
      startCharacter: 5,
      endLine: 4,
      endCharacter: 11,
    });
    expect(contentProvider.initDocumentOutputLanguage).toHaveBeenCalledWith(
      locations[1].uri,
      LanguageName.IL,
    );
  });

  it("returns nothing when the request fails or is cancelled", async () => {
    const { provider, document } = createProvider(async () => {
      throw new Error("Request cancelled");
    });

    expect(
      await provider.provideDefinition(
        document,
        { line: 0, character: 0 } as vscode.Position,
        token,
      ),
    ).toBeUndefined();
  });
});
