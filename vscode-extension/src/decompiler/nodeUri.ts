/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021-2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as path from "path";
import { MemberNode } from "./MemberNode";
import Node from "../protocol/Node";
import NodeData from "../protocol/NodeData";
import { NodeType } from "../protocol/NodeType";

export const ILSPY_URI_SCHEME_LEGACY = "ilspylegacy";
export const ILSPY_URI_SCHEME = "ilspy";

export type NodeFromUri = Node & { name: string };

export function memberNodeToUri(node: MemberNode): vscode.Uri {
  return vscode.Uri.file(path.join(node.assembly, node.name)).with({
    scheme: ILSPY_URI_SCHEME_LEGACY,
    query: [node.rid, node.type, node.memberSubKind, node.parent].join(":"),
  });
}

export function uriToMemberNode(uri: vscode.Uri): MemberNode | undefined {
  if (uri.scheme !== ILSPY_URI_SCHEME_LEGACY) {
    return undefined;
  }

  const assembly = path.dirname(uri.fsPath);
  const name = path.basename(uri.fsPath);
  const [rid, type, memberSubKind, parent] = uri.query.split(":");
  return new MemberNode(
    assembly,
    name,
    rid ? parseInt(rid) : 0,
    type ? parseInt(type) : 0,
    memberSubKind ? parseInt(memberSubKind) : 0,
    parent ? parseInt(parent) : 0
  );
}

export function nodeDataToUri(nodeData: NodeData): vscode.Uri {
  return vscode.Uri.file(
    path.join(nodeData.node?.assemblyPath ?? "", nodeData.symbolName)
  ).with({
    scheme: ILSPY_URI_SCHEME,
    query: [
      nodeData.node?.symbolToken,
      nodeData.node?.type,
      nodeData.node?.parentSymbolToken,
    ].join(":"),
  });
}

export function uriToNode(uri: vscode.Uri): NodeFromUri | undefined {
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
