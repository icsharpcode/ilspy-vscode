import { describe, it, expect, vi } from "vitest";

vi.mock("vscode", () => {
  type MockUri = {
    fsPath: string;
    scheme?: string;
    query?: string;
    with: (opts: Partial<MockUri>) => MockUri;
  };

  return {
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
  };
});

import { nodeDataToUri, uriToNode, ILSPY_URI_SCHEME } from "./nodeUri";
import { NodeType } from "../protocol/NodeType";
import type Node from "../protocol/Node";
import type NodeMetadata from "../protocol/NodeMetadata";
import { SymbolModifiers } from "../protocol/SymbolModifiers";
import { NodeFlags } from "../protocol/NodeFlags";
import type { Uri } from "vscode";

describe("nodeUri utilities", () => {
  it("roundtrips node metadata via URI", () => {
    const metadata: NodeMetadata = {
      assemblyPath: "/path/to/asm.dll",
      name: "MyNamespace.MyType",
      symbolToken: 42,
      type: NodeType.Method,
      parentSymbolToken: 21,
      isDecompilable: true,
    };

    const node: Node = {
      metadata,
      displayName: "",
      description: "",
      mayHaveChildren: false,
      modifiers: SymbolModifiers.None,
      flags: NodeFlags.None,
    };

    const uri = nodeDataToUri(node);
    expect(uri.scheme).toEqual(ILSPY_URI_SCHEME);

    const result = uriToNode(uri);
    expect(result).toBeDefined();
    expect(result!.assemblyPath).toEqual("/path/to/asm.dll");
    expect(result!.name).toEqual("MyNamespace.MyType");
    expect(result!.symbolToken).toEqual(42);
    expect(result!.parentSymbolToken).toEqual(21);
    expect(result!.type).toEqual(NodeType.Method);
    expect(result!.isDecompilable).toBe(true);
  });

  it("handles bundled assembly names and trims slashes", () => {
    const metadata: NodeMetadata = {
      assemblyPath: "/path/to/asm.dll",
      bundledAssemblyName: "sub-directory/bundle",
      name: "MyType",
      symbolToken: 7,
      type: NodeType.Class,
      parentSymbolToken: 1,
      isDecompilable: false,
    };

    const node: Node = {
      metadata,
      displayName: "",
      description: "",
      mayHaveChildren: false,
      modifiers: SymbolModifiers.None,
      flags: NodeFlags.None,
    };

    const uri = nodeDataToUri(node);
    const result = uriToNode(uri)!;
    expect(result.assemblyPath).toEqual("/path/to/asm.dll");
    expect(result.bundledAssemblyName).toEqual("sub-directory/bundle");
    expect(result.name).toEqual("MyType");
    expect(result.isDecompilable).toBe(false);
  });

  it("handles path-alike names correctly", () => {
    const metadata: NodeMetadata = {
      assemblyPath: "/path/to/asm.dll",
      name: "sub-directory/file.xml",
      symbolToken: 0,
      type: NodeType.Resource,
      parentSymbolToken: 0,
      isDecompilable: false,
    };

    const node: Node = {
      metadata,
      displayName: "",
      description: "",
      mayHaveChildren: false,
      modifiers: SymbolModifiers.None,
      flags: NodeFlags.None,
    };

    const uri = nodeDataToUri(node);
    const result = uriToNode(uri)!;
    expect(result.assemblyPath).toEqual("/path/to/asm.dll");
    expect(result.name).toEqual("sub-directory/file.xml");
    expect(result.isDecompilable).toBe(false);
  });

  it("returns undefined for non-ilspy schemes", () => {
    const nonILSpyUri = {
      scheme: "file",
      fsPath: "/a:b",
      query: "1,2,3,0",
    } as unknown as Uri;
    expect(uriToNode(nonILSpyUri)).toBeUndefined();
  });
});
