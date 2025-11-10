/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021-2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as path from "path";
import NodeMetadata from "../protocol/NodeMetadata";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";

export const ILSPY_URI_SCHEME = "ilspy";

export function nodeDataToUri(nodeData: Node): vscode.Uri {
  const assemblyFile = nodeData.metadata?.assemblyPath ?? "";
  const bundledAssemblyName = nodeData.metadata?.bundledAssemblyName;
  return vscode.Uri.file(
    path.join(
      bundledAssemblyName
        ? `${assemblyFile};${bundledAssemblyName}`
        : assemblyFile,
      nodeData.metadata?.name ?? ""
    )
  ).with({
    scheme: ILSPY_URI_SCHEME,
    query: [
      nodeData.metadata?.symbolToken,
      nodeData.metadata?.type,
      nodeData.metadata?.parentSymbolToken,
      nodeData.metadata?.isDecompilable ? "1" : "0",
    ].join(":"),
  });
}

export function uriToNode(uri: vscode.Uri): NodeMetadata | undefined {
  if (uri.scheme !== ILSPY_URI_SCHEME) {
    return undefined;
  }

  const assemblyPathParts = path.dirname(uri.fsPath).split(";");
  const assembly = assemblyPathParts[0];
  const bundledAssemblyName =
    assemblyPathParts.length > 1 ? assemblyPathParts[1] : undefined;
  const name = path.basename(uri.fsPath);
  const [symbolToken, type, parentSymbolToken, isDecompilable] =
    uri.query.split(":");
  return {
    assemblyPath: assembly,
    bundledAssemblyName,
    type: parseInt(type) as NodeType,
    symbolToken: parseInt(symbolToken),
    parentSymbolToken: parseInt(parentSymbolToken),
    isDecompilable: isDecompilable === "1",
    name,
  };
}
