import { TreeItemCollapsibleState } from "vscode";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";
import { NodeFlags } from "../protocol/NodeFlags";

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

export function hasNodeFlag(node: Node, flag: NodeFlags) {
  return (node.flags & flag) === flag;
}
