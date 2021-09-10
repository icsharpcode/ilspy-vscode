/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { memberNodeToUri } from "../decompiler/memberNodeUri";
import { MemberNode } from "../decompiler/MemberNode";
import { languageInfos } from "../decompiler/languageInfos";
import { getDefaultOutputLanguage } from "../decompiler/settings";

let lastSelectedNode: MemberNode | undefined = undefined;

export function registerShowCode(
  decompiledTreeProvider: DecompiledTreeProvider
) {
  return vscode.commands.registerCommand(
    "showCode",
    async (node: MemberNode) => {
      if (lastSelectedNode === node) {
        return;
      }

      lastSelectedNode = node;

      let doc = await vscode.workspace.openTextDocument(memberNodeToUri(node));
      vscode.languages.setTextDocumentLanguage(
        doc,
        languageInfos[getDefaultOutputLanguage()].vsLanguageMode
      );
      await vscode.window.showTextDocument(doc, { preview: true });
    }
  );
}
