/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2024 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";

export function registerSearchEditorSelection() {
  return vscode.commands.registerCommand(
    "ilspy.searchEditorSelection",
    async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        vscode.window.showWarningMessage("Editor undefined!");
        return;
      }

      const selection = editor.selection;
      if (selection.isEmpty) {
        vscode.window.showWarningMessage("Selection empty!");
        return;
      }

      const selectionRange = new vscode.Range(
        selection.start.line,
        selection.start.character,
        selection.end.line,
        selection.end.character
      );
      const selectedText = editor.document.getText(selectionRange);
      vscode.commands.executeCommand("ilspy.search", selectedText);
    }
  );
}
