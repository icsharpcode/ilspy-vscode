import * as vscode from "vscode";
import NodeMetadata from "../protocol/NodeMetadata";

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