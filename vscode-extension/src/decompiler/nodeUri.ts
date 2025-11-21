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
      ":",
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

  const assemblyPathAndNameParts = uri.fsPath.split(":");
  const name =
    assemblyPathAndNameParts.length > 1 ? assemblyPathAndNameParts[1] : "";

  const assemblyPathParts = assemblyPathAndNameParts[0].split(";");
  const assembly = assemblyPathParts[0];
  const bundledAssemblyName =
    assemblyPathParts.length > 1 ? assemblyPathParts[1] : undefined;

  const [symbolToken, type, parentSymbolToken, isDecompilable] =
    uri.query.split(":");
  return {
    assemblyPath: trimTrailingSlashes(assembly),
    bundledAssemblyName: bundledAssemblyName
      ? trimTrailingSlashes(bundledAssemblyName)
      : undefined,
    type: parseInt(type) as NodeType,
    symbolToken: parseInt(symbolToken),
    parentSymbolToken: parseInt(parentSymbolToken),
    isDecompilable: isDecompilable === "1",
    name: trimLeadingSlashes(name),
  };
}

function trimLeadingSlashes(input: string) {
  let result = input;
  if (result.startsWith("/")) {
    result = result.substring(1);
  }
  return result;
}

function trimTrailingSlashes(input: string) {
  let result = input;
  if (result.endsWith("/")) {
    result = result.slice(0, -1);
  }
  return result;
}
