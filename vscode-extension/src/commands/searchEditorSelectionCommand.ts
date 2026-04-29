/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2024 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { executeILSpyCommand, registerILSpyCommand } from "./commandUtils";

export function registerSearchEditorSelectionCommand() {
  return registerILSpyCommand("ilspy.searchEditorSelection", async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
      return;
    }

    const wordRange = editor.document.getWordRangeAtPosition(
      editor.selection.start,
    );
    const selectedText = editor.document.getText(wordRange);
    await executeILSpyCommand("ilspy.search", selectedText);
  });
}
