import { describe, expect, it, vi } from "vitest";

vi.mock("vscode", () => {
  class EventEmitter {
    event = () => {};
    fire = vi.fn();
  }
  class ThemeIcon {
    constructor(
      public id: string,
      public color?: unknown,
    ) {}
  }
  class ThemeColor {
    constructor(public id: string) {}
  }
  class MarkdownString {
    constructor(public value = "") {}
    appendMarkdown(value: string) {
      this.value += value;
      return this;
    }
    appendText(value: string) {
      return this.appendMarkdown(value);
    }
  }
  return {
    EventEmitter,
    ThemeIcon,
    ThemeColor,
    MarkdownString,
    TreeItemCollapsibleState: { None: 0, Collapsed: 1, Expanded: 2 },
    commands: { executeCommand: vi.fn() },
    workspace: { getConfiguration: () => ({ get: () => false }) },
    Uri: { file: (p: string) => ({ fsPath: p }), joinPath: vi.fn() },
  };
});

import { commands } from "vscode";
import type IILSpyBackend from "../IILSpyBackend";
import { NodeType } from "../../extension-types";
import {
  isPerformedSearchNode,
  SearchResultTreeProvider,
  SearchTreeNode,
} from "./SearchResultTreeProvider";

function createProvider() {
  const backend = {
    sendSearch: vi.fn(async ({ term }: { term: string }) => ({
      results: [
        {
          metadata: {
            assemblyPath: "/a.dll",
            type: NodeType.Method,
            name: term,
            symbolToken: 1,
            parentSymbolToken: 0,
            availableCommands: 0,
          },
          displayName: term,
          description: "",
          mayHaveChildren: false,
          symbolModifiers: 0,
          flags: 0,
        },
      ],
      shouldUpdateAssemblyList: false,
    })),
  } as unknown as IILSpyBackend;
  return new SearchResultTreeProvider(backend);
}

function searches(provider: SearchResultTreeProvider) {
  return (provider.getChildren() as SearchTreeNode[]).filter(
    isPerformedSearchNode,
  );
}

function resultIds(provider: SearchResultTreeProvider) {
  return searches(provider).flatMap((search) =>
    search.results.map((result) => provider.getTreeItem(result).id),
  );
}

describe("SearchResultTreeProvider", () => {
  it("marks performed searches so they get the clear action", async () => {
    const provider = createProvider();
    await provider.performSearch("foo");
    const [search] = searches(provider);
    expect(provider.getTreeItem(search).contextValue).toBe("performedSearch");
  });

  it("removes a single search and keeps the others", async () => {
    const provider = createProvider();
    await provider.performSearch("foo");
    await provider.performSearch("bar");
    const fooSearch = searches(provider).find((s) => s.term === "foo")!;

    provider.removeSearch(fooSearch);

    expect(searches(provider).map((s) => s.term)).toEqual(["bar"]);
    expect(commands.executeCommand).toHaveBeenLastCalledWith(
      "setContext",
      "ilspy.hasSearchResults",
      true,
    );
  });

  it("clears all searches", async () => {
    const provider = createProvider();
    await provider.performSearch("foo");
    await provider.performSearch("bar");

    provider.clearSearches();

    expect(searches(provider)).toEqual([]);
    expect(commands.executeCommand).toHaveBeenLastCalledWith(
      "setContext",
      "ilspy.hasSearchResults",
      false,
    );
  });

  it("keeps result IDs unique after removing a search", async () => {
    const provider = createProvider();
    await provider.performSearch("same");
    await provider.performSearch("same");
    provider.removeSearch(searches(provider).at(-1)!);
    await provider.performSearch("same");

    const ids = resultIds(provider);
    expect(new Set(ids).size).toBe(ids.length);
  });
});
