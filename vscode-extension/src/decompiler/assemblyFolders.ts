/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as path from "path";
import { Node, NodeFlags, SymbolModifiers } from "../extension-types";

/**
 * Folder node grouping root entries (assemblies, packages) of the assembly tree by their location
 * on disk. Folder nodes exist only in the extension's tree view, the backend doesn't know them.
 */
export interface AssemblyFolderNode extends Node {
  folderPath: string;
  folderChildren: Node[];
  parentFolder?: AssemblyFolderNode;
  // Whether the folder (or a sub-folder) contains entries loaded by the user, not only auto-loaded dependencies.
  containsLoadedEntries: boolean;
}

export function isAssemblyFolderNode(
  node: Node | undefined,
): node is AssemblyFolderNode {
  return !!node && "folderPath" in node;
}

interface Directory {
  path: string;
  subDirectories: Map<string, Directory>;
  entries: Node[];
}

/**
 * Groups root entries by the folders containing their files. Chains of folders without own entries
 * are compacted into a single folder node (like VS Code's compact folders), so the top level shows
 * the deepest folder containing all entries below it. Folders come before entries, both sorted by name.
 */
export function groupByFolder(
  rootEntries: Node[],
  options: { pathApi?: path.PlatformPath; homeDir?: string } = {},
): Node[] {
  const pathApi = options.pathApi ?? path;
  const roots = new Map<string, Directory>();
  const ungrouped: Node[] = [];

  for (const entry of rootEntries) {
    const filePath = entry.metadata?.assemblyPath;
    if (!filePath) {
      ungrouped.push(entry);
      continue;
    }

    const dirPath = pathApi.dirname(filePath);
    const rootPath = pathApi.parse(dirPath).root;
    let dir = getOrAdd(roots, rootPath, rootPath);
    for (const segment of dirPath
      .slice(rootPath.length)
      .split(pathApi.sep)
      .filter((s) => s.length > 0)) {
      dir = getOrAdd(
        dir.subDirectories,
        segment,
        pathApi.join(dir.path, segment),
      );
    }
    dir.entries.push(entry);
  }

  const topLevelDirs = [...roots.values()].flatMap((root) => {
    const compacted = compact(root);
    return compacted !== root || root.entries.length > 0
      ? [compacted]
      : [...root.subDirectories.values()].map(compact);
  });

  const topLevelFolders = topLevelDirs.map((dir) =>
    createFolderNode(
      dir,
      shortenHomeDir(dir.path, pathApi, options.homeDir),
      pathApi,
    ),
  );
  return [...sortByName(topLevelFolders), ...sortByName(ungrouped)];
}

/**
 * Lists all root entries contained in the given nodes (folders and their sub-folders).
 */
export function collectFolderEntries(
  nodes: Node[],
): { entry: Node; folder?: AssemblyFolderNode }[] {
  return nodes.flatMap((node) =>
    isAssemblyFolderNode(node)
      ? node.folderChildren.flatMap((child) =>
          isAssemblyFolderNode(child)
            ? collectFolderEntries([child])
            : [{ entry: child, folder: node }],
        )
      : [{ entry: node }],
  );
}

function getOrAdd(
  map: Map<string, Directory>,
  key: string,
  dirPath: string,
): Directory {
  let dir = map.get(key);
  if (!dir) {
    dir = { path: dirPath, subDirectories: new Map(), entries: [] };
    map.set(key, dir);
  }
  return dir;
}

function compact(dir: Directory): Directory {
  while (dir.entries.length === 0 && dir.subDirectories.size === 1) {
    dir = dir.subDirectories.values().next().value!;
  }
  return dir;
}

function createFolderNode(
  dir: Directory,
  label: string,
  pathApi: path.PlatformPath,
  parentFolder?: AssemblyFolderNode,
): AssemblyFolderNode {
  const folder: AssemblyFolderNode = {
    displayName: label,
    description: "",
    mayHaveChildren: true,
    modifiers: SymbolModifiers.None,
    flags: NodeFlags.None,
    folderPath: dir.path,
    folderChildren: [],
    parentFolder,
    containsLoadedEntries: false,
  };
  const subFolders = [...dir.subDirectories.values()].map((subDir) => {
    const compacted = compact(subDir);
    return createFolderNode(
      compacted,
      pathApi.relative(dir.path, compacted.path),
      pathApi,
      folder,
    );
  });
  folder.folderChildren = [
    ...sortByName(subFolders),
    ...sortByName(dir.entries),
  ];
  folder.containsLoadedEntries =
    subFolders.some((subFolder) => subFolder.containsLoadedEntries) ||
    dir.entries.some((entry) => (entry.flags & NodeFlags.AutoLoaded) === 0);
  return folder;
}

function shortenHomeDir(
  dirPath: string,
  pathApi: path.PlatformPath,
  homeDir: string | undefined,
) {
  if (!homeDir) {
    return dirPath;
  }
  if (dirPath === homeDir) {
    return "~";
  }
  const homePrefix = homeDir.endsWith(pathApi.sep)
    ? homeDir
    : homeDir + pathApi.sep;
  return dirPath.startsWith(homePrefix)
    ? "~" + pathApi.sep + dirPath.slice(homePrefix.length)
    : dirPath;
}

function sortByName<T extends Node>(nodes: T[]): T[] {
  return [...nodes].sort((a, b) =>
    a.displayName.localeCompare(b.displayName, undefined, {
      sensitivity: "base",
      numeric: true,
    }),
  );
}
