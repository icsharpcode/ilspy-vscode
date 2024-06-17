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
        return;
      }

      const selection = editor.selection;
      if (selection.isEmpty) {
        await vscode.commands.executeCommand(
          "editor.action.smartSelect.expand"
        );
      }

      const expandedSelection = editor.selection;
      const selectionRange = new vscode.Range(
        expandedSelection.start.line,
        expandedSelection.start.character,
        expandedSelection.end.line,
        expandedSelection.end.character
      );
      const selectedText = editor.document.getText(selectionRange);
      vscode.commands.executeCommand("ilspy.search", selectedText);
    }
  );
}
