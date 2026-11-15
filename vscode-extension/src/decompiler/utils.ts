import { MarkdownString, TreeItemCollapsibleState } from "vscode";
import {
  AvailableNodeCommands,
  Node,
  NodeFlags,
  NodeType,
} from "../extension-types";

export const ASSEMBLY_FILE_EXTENSIONS = [
  "dll",
  "exe",
  "winmd",
  "netmodule",
  "wasm",
  "nupkg",
];

export function isTypeNode(nodeType: NodeType) {
  return (
    nodeType === NodeType.Class ||
    nodeType === NodeType.Enum ||
    nodeType === NodeType.Delegate ||
    nodeType === NodeType.Interface ||
    nodeType === NodeType.Struct
  );
}

export function getTreeNodeCollapsibleState(
  node: Node,
  expandByDefault = false
) {
  return node.mayHaveChildren
    ? expandByDefault
      ? TreeItemCollapsibleState.Expanded
      : TreeItemCollapsibleState.Collapsed
    : TreeItemCollapsibleState.None;
}

/**
 * Checks whether two nodes represent the same tree entry, even if they come from different
 * sources (e.g. a search result and the assembly tree) and differ in display name.
 */
export function isSameNode(a: Node, b: Node) {
  const am = a.metadata;
  const bm = b.metadata;
  if (!am || !bm) {
    return false;
  }

  return (
    am.assemblyPath === bm.assemblyPath &&
    (am.bundledAssemblyName ?? "") === (bm.bundledAssemblyName ?? "") &&
    am.type === bm.type &&
    am.symbolToken === bm.symbolToken &&
    am.parentSymbolToken === bm.parentSymbolToken &&
    // Nodes without symbol token (assemblies, namespaces, ...) are identified by name.
    (am.symbolToken !== 0 || am.name === bm.name)
  );
}

export function createNodeId(node: Node) {
  const metadata = node.metadata;
  if (metadata && metadata.symbolToken !== 0) {
    return [
      metadata.assemblyPath,
      metadata.bundledAssemblyName,
      metadata.type,
      metadata.symbolToken,
      metadata.parentSymbolToken,
    ].join(":");
  }

  return undefined;
}

export function getNodeContextValue(node: Node) {
  let contextValue = "";

  if (hasNodeCommand(node, AvailableNodeCommands.ManageRootEntries)) {
    contextValue += "#manageRootEntries";
  }
  if (hasNodeCommand(node, AvailableNodeCommands.Analyze)) {
    contextValue += "#analyze";
  }
  if (hasNodeCommand(node, AvailableNodeCommands.Export)) {
    contextValue += "#export";
  }

  return contextValue;
}

export function createNodeTooltip(node: Node) {
  const tooltip = new MarkdownString();
  const description = node.description.trim();
  if (description.length > 0) {
    tooltip.appendMarkdown("**");
    tooltip.appendText(node.displayName);
    tooltip.appendMarkdown("**\n\n");
    tooltip.appendText(description);
  } else {
    tooltip.appendText(node.displayName);
  }

  return tooltip;
}

export function hasNodeFlag(node: Node, flag: NodeFlags) {
  return (node.flags & flag) === flag;
}

export function hasNodeCommand(node: Node, flag: AvailableNodeCommands) {
  return !!node.metadata && (node.metadata.availableCommands & flag) === flag;
}
