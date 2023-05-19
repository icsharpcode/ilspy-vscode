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
  return vscode.Uri.file(
    path.join(
      nodeData.metadata?.assemblyPath ?? "",
      nodeData.metadata?.name ?? ""
    )
  ).with({
    scheme: ILSPY_URI_SCHEME,
    query: [
      nodeData.metadata?.symbolToken,
      nodeData.metadata?.type,
      nodeData.metadata?.parentSymbolToken,
    ].join(":"),
  });
}

export function uriToNode(uri: vscode.Uri): NodeMetadata | undefined {
  if (uri.scheme !== ILSPY_URI_SCHEME) {
    return undefined;
  }

  const assembly = path.dirname(uri.fsPath);
  const name = path.basename(uri.fsPath);
  const [symbolToken, type, parentSymbolToken] = uri.query.split(":");
  return {
    assemblyPath: assembly,
    type: parseInt(type) as NodeType,
    symbolToken: parseInt(symbolToken),
    parentSymbolToken: parseInt(parentSymbolToken),
    name,
  };
}
