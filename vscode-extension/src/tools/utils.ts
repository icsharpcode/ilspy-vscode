import * as vscode from "vscode";
import NodeMetadata from "../protocol/NodeMetadata";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";

export function parseILSpyUri(uriString: string): NodeMetadata | undefined {
  try {
    const uri = vscode.Uri.parse(uriString);
    if (uri.scheme !== "ilspy") {
      return undefined;
    }

    const assemblyPath = uri.path.substring(0, uri.path.lastIndexOf("/"));
    const name = uri.path.substring(uri.path.lastIndexOf("/") + 1);

    if (!uri.query) {
      return undefined;
    }

    const [symbolToken, type, parentSymbolToken, isDecompilable] = uri.query.split(":");

    return {
      assemblyPath,
      type: parseInt(type),
      name,
      symbolToken: parseInt(symbolToken),
      parentSymbolToken: parseInt(parentSymbolToken),
      isDecompilable: isDecompilable === "1",
    };
  } catch {
    return undefined;
  }
}

const SYMBOL_TYPE_MAP: Record<string, NodeType[]> = {
  type: [NodeType.Class, NodeType.Interface, NodeType.Struct, NodeType.Enum, NodeType.Delegate],
  method: [NodeType.Method],
  field: [NodeType.Field],
  property: [NodeType.Property],
  event: [NodeType.Event],
};

/**
 * Filters nodes by symbol type based on the NodeType metadata
 */
export function filterNodesBySymbolType(
  nodes: Node[],
  symbolType?: "type" | "method" | "field" | "property" | "event" | "all"
): Node[] {
  if (!symbolType || symbolType === "all") {
    return nodes;
  }

  const allowedTypes = SYMBOL_TYPE_MAP[symbolType];
  if (!allowedTypes) {
    return nodes;
  }

  return nodes.filter((node) => 
    node.metadata && allowedTypes.includes(node.metadata.type)
  );
}