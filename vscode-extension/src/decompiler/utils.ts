import { MarkdownString, TreeItemCollapsibleState } from "vscode";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";
import { NodeFlags } from "../protocol/NodeFlags";
import { AvailableNodeCommands } from "../protocol/AvailableNodeCommands";
import NodeMetadata from "../protocol/NodeMetadata";

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
