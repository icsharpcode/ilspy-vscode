/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2024 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";

export function registerSearchEditorSelectionCommand() {
  return vscode.commands.registerCommand(
    "ilspy.searchEditorSelection",
    async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        return;
      }

      const wordRange = editor.document.getWordRangeAtPosition(
        editor.selection.start
      );
      const selectedText = editor.document.getText(wordRange);
      vscode.commands.executeCommand("ilspy.search", selectedText);
    }
  );
}
