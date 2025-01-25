/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { nodeDataToUri } from "../decompiler/nodeUri";
import { languageInfos } from "../decompiler/languageInfos";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import Node from "../protocol/Node";

let lastSelectedNode: Node | undefined = undefined;

export function registerDecompileNodeCommand(
  contentProvider: DecompilerTextDocumentContentProvider
) {
  return vscode.commands.registerCommand(
    "ilspy.decompileNode",
    async (node: Node) => {
      if (lastSelectedNode === node) {
        return;
      }

      lastSelectedNode = node;

      const uri = nodeDataToUri(node);
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
