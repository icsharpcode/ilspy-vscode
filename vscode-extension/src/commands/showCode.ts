/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { memberNodeToUri } from "../decompiler/memberNodeUri";
import { MemberNode } from "../decompiler/MemberNode";
import { languageInfos } from "../decompiler/languageInfos";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";

let lastSelectedNode: MemberNode | undefined = undefined;

export function registerShowCode(
  contentProvider: DecompilerTextDocumentContentProvider
) {
  return vscode.commands.registerCommand(
    "showCode",
    async (node: MemberNode) => {
      if (lastSelectedNode === node) {
        return;
      }

      lastSelectedNode = node;

      const uri = memberNodeToUri(node);
      const language = getDefaultOutputLanguage();

      contentProvider.setDocumentOutputLanguage(uri, language);

      let doc = await vscode.workspace.openTextDocument(uri);
      vscode.languages.setTextDocumentLanguage(
        doc,
        languageInfos[language].vsLanguageMode
      );
      await vscode.window.showTextDocument(doc, { preview: true });
    }
  );
}
