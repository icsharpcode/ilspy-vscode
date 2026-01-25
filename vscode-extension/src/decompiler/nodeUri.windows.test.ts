import { it, expect, vi } from "vitest";

vi.resetModules();

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

vi.mock("path", () => {
  return {
    sep: "\\\\",
    join: (...parts: string[]) => parts.join("\\\\"),
  };
});

import { nodeDataToUri, uriToNode } from "./nodeUri";
import { NodeType } from "../protocol/NodeType";
import type Node from "../protocol/Node";
import type NodeMetadata from "../protocol/NodeMetadata";
import { SymbolModifiers } from "../protocol/SymbolModifiers";
import { NodeFlags } from "../protocol/NodeFlags";

it("handles Windows-style backslashes when path.sep is '\\'", () => {
  const metadata: NodeMetadata = {
    assemblyPath: "C:\\path\\to\\asm.dll",
    bundledAssemblyName: "sub-directory/bundle",
    name: "MyType",
    symbolToken: 9,
    type: NodeType.Enum,
    parentSymbolToken: 2,
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
  const md = uriToNode(uri)!;
  expect(md.assemblyPath).toEqual("C:\\path\\to\\asm.dll");
  expect(md.bundledAssemblyName).toEqual("sub-directory/bundle");
  expect(md.name).toEqual("MyType");
});
