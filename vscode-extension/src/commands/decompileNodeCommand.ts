/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import { languageInfos } from "../decompiler/languageInfos";
import { nodeDataToUri } from "../decompiler/nodeUri";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import { hasNodeCommand } from "../decompiler/utils";
import { executeILSpyCommand, registerILSpyCommand } from "./commandUtils";
import { AvailableNodeCommands, Node, SearchMatch } from "../extension-types";
import { findSearchMatch } from "../decompiler/search/searchMatch";

export function registerDecompileNodeCommand(
  contentProvider: DecompilerTextDocumentContentProvider,
) {
  return registerILSpyCommand(
    "ilspy.decompileNode",
    async (node: Node, revealInTree = false, match?: SearchMatch) => {
      const uri = nodeDataToUri(node);
      if (hasNodeCommand(node, AvailableNodeCommands.Decompile)) {
        const language = getDefaultOutputLanguage();

        contentProvider.setDocumentOutputLanguage(uri, language);

        let doc = await vscode.workspace.openTextDocument(uri);

        vscode.languages.setTextDocumentLanguage(
          doc,
          languageInfos[language].vsLanguageMode,
        );
        const editor = await vscode.window.showTextDocument(doc, {
          preview: true,
        });
        if (match) {
          selectSearchMatch(editor, match);
        }
      }

      if (revealInTree) {
        await executeILSpyCommand("ilspy.revealNode", node);
      }
    },
  );
}

function selectSearchMatch(editor: vscode.TextEditor, match: SearchMatch) {
  const position = findSearchMatch(editor.document.getText(), match);
  if (!position) {
    return;
  }

  const range = new vscode.Range(
    position.line,
    position.column,
    position.line,
    position.column + position.length,
  );
  editor.selection = new vscode.Selection(range.start, range.end);
  editor.revealRange(
    range,
    vscode.TextEditorRevealType.InCenterIfOutsideViewport,
  );
}
