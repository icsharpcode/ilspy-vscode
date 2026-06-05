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
import { AvailableNodeCommands, Node } from "../extension-types";

export function registerDecompileNodeCommand(
  contentProvider: DecompilerTextDocumentContentProvider,
) {
  return registerILSpyCommand(
    "ilspy.decompileNode",
    async (node: Node, revealInTree = false) => {
      const uri = nodeDataToUri(node);
      if (hasNodeCommand(node, AvailableNodeCommands.Decompile)) {
        const language = getDefaultOutputLanguage();

        contentProvider.setDocumentOutputLanguage(uri, language);

        let doc =
          vscode.workspace.textDocuments.find(
            (d) => d.uri.toString() === uri.toString(),
          ) ?? (await vscode.workspace.openTextDocument(uri));

        vscode.languages.setTextDocumentLanguage(
          doc,
          languageInfos[language].vsLanguageMode,
        );
        await vscode.window.showTextDocument(doc, { preview: true });
      }

      if (revealInTree) {
        await executeILSpyCommand("ilspy.revealNode", node);
      }
    },
  );
}
