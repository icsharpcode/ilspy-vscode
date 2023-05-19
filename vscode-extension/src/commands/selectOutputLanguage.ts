/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import { languageInfos } from "../decompiler/languageInfos";
import { ILSPY_URI_SCHEME } from "../decompiler/nodeUri";
import { setDefaultOutputLanguage } from "../decompiler/settings";
import { LanguageName } from "../protocol/DecompileResponse";

type OutputLanguageQuickPickItem = vscode.QuickPickItem & {
  languageName: string;
  isActive: boolean;
};

export function registerSelectOutputLanguage(
  contentProvider: DecompilerTextDocumentContentProvider
) {
  return vscode.commands.registerCommand(
    "ilspy.selectOutputLanguage",
    async () => {
      let document = vscode.window.activeTextEditor?.document;
      if (document === undefined) {
        return;
      }
      const scheme = document?.uri.scheme;
      if (scheme !== ILSPY_URI_SCHEME) {
        return;
      }

      const language = await vscode.window.showQuickPick(
        Object.entries(languageInfos)
          .map((languageInfoEntry) => {
            const [languageName, languageInfo] = languageInfoEntry;
            const isActive =
              contentProvider.getDocumentOutputLanguage(document?.uri) ===
              languageName;
            return {
              label: languageInfo.displayName,
              description: isActive ? "active" : undefined,
              languageName,
              isActive,
            } as OutputLanguageQuickPickItem;
          })
          .sort((item1, item2) => {
            if (item1.isActive && !item2.isActive) {
              return -1;
            } else if (!item1.isActive && item2.isActive) {
              return 1;
            }

            return item1.label.localeCompare(item2.label);
          }),
        {
          title: "Please select language of decompiled code output",
          matchOnDescription: false,
        }
      );
      if (language) {
        const languageInfo = languageInfos[language.languageName];
        setDefaultOutputLanguage(languageInfo);
        contentProvider.setDocumentOutputLanguage(
          document.uri,
          language.languageName as LanguageName
        );
        vscode.languages.setTextDocumentLanguage(
          document,
          languageInfo.vsLanguageMode
        );
      }
    }
  );
}
