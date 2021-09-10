/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import { languageInfos } from "../decompiler/languageInfos";
import { ILSPY_URI_SCHEME } from "../decompiler/memberNodeUri";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import { LanguageName } from "../protocol/DecompileResponse";

type OutputLanguageQuickPickItem = vscode.QuickPickItem & {
  languageName: string;
};

export function registerSelectOutputLanguage(
  contentProvider: DecompilerTextDocumentContentProvider
) {
  return vscode.commands.registerCommand(
    "ilspy.selectOutputLanguage",
    async () => {
      let document = vscode.window.activeTextEditor?.document;
      if (document?.uri.scheme !== ILSPY_URI_SCHEME) {
        return;
      }

      const language = await vscode.window.showQuickPick(
        Object.entries(languageInfos).map((languageInfoEntry) => {
          const [languageName, languageInfo] = languageInfoEntry;
          return {
            label: languageInfo.displayName,
            description:
              getDefaultOutputLanguage() === languageName
                ? "default"
                : undefined,
            languageName,
          } as OutputLanguageQuickPickItem;
        }),
        {
          title: "Please select language of decompiled code output",
        }
      );
      if (language) {
        contentProvider.setDocumentOutputLanguage(
          document.uri,
          language.languageName as LanguageName
        );
        vscode.languages.setTextDocumentLanguage(
          document,
          languageInfos[language.languageName].vsLanguageMode
        );
      }
    }
  );
}
