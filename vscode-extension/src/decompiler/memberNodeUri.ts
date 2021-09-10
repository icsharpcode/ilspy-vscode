/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as path from "path";
import { MemberNode } from "./MemberNode";

export const ILSPY_URI_SCHEME = "ilspy";

export function memberNodeToUri(node: MemberNode): vscode.Uri {
  return vscode.Uri.file(path.join(node.assembly, node.name)).with({
    scheme: ILSPY_URI_SCHEME,
    query: [node.rid, node.type, node.memberSubKind, node.parent].join(":"),
  });
}

export function uriToMemberNode(uri: vscode.Uri): MemberNode | undefined {
  if (uri.scheme !== ILSPY_URI_SCHEME) {
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
