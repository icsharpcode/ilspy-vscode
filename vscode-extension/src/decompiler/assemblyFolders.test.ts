import * as path from "path";
import { describe, expect, it } from "vitest";
import {
  AssemblyFolderNode,
  collectFolderEntries,
  groupByFolder,
  isAssemblyFolderNode,
} from "./assemblyFolders";
import { Node, NodeFlags, NodeType, SymbolModifiers } from "../extension-types";

function assembly(assemblyPath: string, flags = NodeFlags.None): Node {
  const fileName = (
    assemblyPath.includes("\\") ? path.win32 : path.posix
  ).basename(assemblyPath);
  return {
    metadata: {
      assemblyPath,
      type: NodeType.Assembly,
      name: fileName,
      symbolToken: 0,
      parentSymbolToken: 0,
      availableCommands: 0,
    },
    displayName: fileName,
    description: assemblyPath,
    mayHaveChildren: true,
    modifiers: SymbolModifiers.None,
    flags,
  };
}

// Simplified view of the tree: folders as { label: children }, entries as their display name.
type Outline = (string | { [label: string]: Outline })[];
function outline(nodes: Node[]): Outline {
  return nodes.map((node) =>
    isAssemblyFolderNode(node)
      ? { [node.displayName]: outline(node.folderChildren) }
      : node.displayName,
  );
}

const app = "/Users/me/workspace/RetailerApp/asiacellapi";
const framework = "/usr/local/share/dotnet/shared/Microsoft.NETCore.App/10.0.0";

describe("groupByFolder", () => {
  it("puts all assemblies of one folder into a single compacted folder node", () => {
    const nodes = groupByFolder([
      assembly(`${app}/RetailerApp.dll`),
      assembly(`${app}/RetailerApp.Common.dll`),
    ]);

    expect(outline(nodes)).toEqual([
      { [app]: ["RetailerApp.Common.dll", "RetailerApp.dll"] },
    ]);
  });

  it("separates unrelated folders and sorts folders and assemblies by name", () => {
    const nodes = groupByFolder([
      assembly(`${app}/RetailerApp.dll`),
      assembly(`${framework}/System.Runtime.dll`),
      assembly(`${app}/RetailerApp.Actions.dll`),
      assembly(`${framework}/System.Collections.dll`),
    ]);

    expect(outline(nodes)).toEqual([
      { [app]: ["RetailerApp.Actions.dll", "RetailerApp.dll"] },
      { [framework]: ["System.Collections.dll", "System.Runtime.dll"] },
    ]);
  });

  it("nests sub-folders below their common folder, folders first", () => {
    const nodes = groupByFolder([
      assembly(`/Users/me/workspace/RetailerApp/other/lib/Other.dll`),
      assembly(`${app}/RetailerApp.dll`),
      assembly(`/Users/me/workspace/RetailerApp/Root.dll`),
    ]);

    expect(outline(nodes)).toEqual([
      {
        "/Users/me/workspace/RetailerApp": [
          { asiacellapi: ["RetailerApp.dll"] },
          { "other/lib": ["Other.dll"] },
          "Root.dll",
        ],
      },
    ]);
  });

  it("shortens the home directory of top-level folders to ~", () => {
    const nodes = groupByFolder([assembly(`${app}/RetailerApp.dll`)], {
      homeDir: "/Users/me",
    });

    expect(nodes[0].displayName).toBe("~/workspace/RetailerApp/asiacellapi");
    expect((nodes[0] as AssemblyFolderNode).folderPath).toBe(app);
  });

  it("supports Windows paths", () => {
    const nodes = groupByFolder(
      [
        assembly("C:\\Projects\\App\\bin\\App.dll"),
        assembly("C:\\Projects\\App\\bin\\App.Core.dll"),
        assembly("D:\\Libs\\Lib.dll"),
      ],
      { pathApi: path.win32, homeDir: "C:\\Users\\me" },
    );

    expect(outline(nodes)).toEqual([
      { "C:\\Projects\\App\\bin": ["App.Core.dll", "App.dll"] },
      { "D:\\Libs": ["Lib.dll"] },
    ]);
  });

  it("keeps entries without file path ungrouped", () => {
    const withoutPath = { ...assembly("/x/A.dll"), metadata: undefined };

    expect(outline(groupByFolder([withoutPath]))).toEqual(["A.dll"]);
  });

  it("links folders to their parents and lists entries with their folder", () => {
    const nodes = groupByFolder([
      assembly(`${app}/RetailerApp.dll`),
      assembly(`/Users/me/workspace/RetailerApp/Root.dll`),
    ]);
    const entries = collectFolderEntries(nodes);

    expect(
      entries.map(({ entry, folder }) => [
        entry.displayName,
        folder?.folderPath,
      ]),
    ).toEqual([
      ["RetailerApp.dll", app],
      ["Root.dll", "/Users/me/workspace/RetailerApp"],
    ]);
    expect(entries[0].folder?.parentFolder).toBe(nodes[0]);
    expect((nodes[0] as AssemblyFolderNode).parentFolder).toBeUndefined();
  });

  it("marks folders containing user-loaded entries, unlike dependency-only folders", () => {
    const nodes = groupByFolder([
      assembly(`${app}/RetailerApp.dll`),
      assembly(`${app}/Newtonsoft.Json.dll`, NodeFlags.AutoLoaded),
      assembly(`${framework}/System.Runtime.dll`, NodeFlags.AutoLoaded),
      assembly(
        "/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.0/Microsoft.AspNetCore.dll",
        NodeFlags.AutoLoaded,
      ),
    ]) as AssemblyFolderNode[];

    const appFolder = nodes.find((f) => f.folderPath === app)!;
    const sharedFolder = nodes.find(
      (f) => f.folderPath === "/usr/local/share/dotnet/shared",
    )!;
    expect(appFolder.containsLoadedEntries).toBe(true);
    expect(sharedFolder.containsLoadedEntries).toBe(false);
    expect(
      (sharedFolder.folderChildren as AssemblyFolderNode[]).map(
        (f) => f.containsLoadedEntries,
      ),
    ).toEqual([false, false]);
  });

  it("marks parent folders of folders with user-loaded entries", () => {
    const nodes = groupByFolder([
      assembly(`${app}/RetailerApp.dll`),
      assembly(
        "/Users/me/workspace/RetailerApp/deps/Dep.dll",
        NodeFlags.AutoLoaded,
      ),
    ]) as AssemblyFolderNode[];

    expect(nodes[0].folderPath).toBe("/Users/me/workspace/RetailerApp");
    expect(nodes[0].containsLoadedEntries).toBe(true);
  });
});
