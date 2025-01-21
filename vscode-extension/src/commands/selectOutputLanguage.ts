/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import {
  languageInfos,
  LATEST_OUTPUT_LANGUAGE,
} from "../decompiler/languageInfos";
import { ILSPY_URI_SCHEME } from "../decompiler/nodeUri";
import { setDefaultOutputLanguage } from "../decompiler/settings";
import { LanguageName } from "../protocol/LanguageName";

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

      const currentDocumentOutputLanguage =
        contentProvider.getDocumentOutputLanguage(document?.uri);
      const languageInfoEntries = Object.entries(languageInfos);
      const language = await vscode.window.showQuickPick(
        languageInfoEntries
          .sort((languageInfo1, languageInfo2) => {
            const langInfo1Active =
              languageInfo1[0] === currentDocumentOutputLanguage;
            const langInfo2Active =
              languageInfo2[0] === currentDocumentOutputLanguage;
            if (langInfo1Active && !langInfo2Active) {
              return -1;
            } else if (!langInfo1Active && langInfo2Active) {
              return 1;
            }

            return (
              languageInfoEntries.indexOf(languageInfo2) -
              languageInfoEntries.indexOf(languageInfo1)
            );
          })
          .map((languageInfoEntry) => {
            const [languageName, languageInfo] = languageInfoEntry;
            const isActive =
              contentProvider.getDocumentOutputLanguage(document?.uri) ===
              languageName;
            const labels = [];
            if (isActive) {
              labels.push("active");
            }
            if (LATEST_OUTPUT_LANGUAGE === languageName) {
              labels.push("latest");
            }
            return {
              label: languageInfo.displayName,
              description: labels.join(" / "),
              languageName,
              isActive,
            } as OutputLanguageQuickPickItem;
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
        updateSelectOutputLanguageStatusBarItem(contentProvider);
      }
    }
  );
}

let selectOutputLanguageStatusBarItem: vscode.StatusBarItem;

export function registerSelectOutputLanguageStatusBarItem(
  contentProvider: DecompilerTextDocumentContentProvider
) {
  selectOutputLanguageStatusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100
  );
  selectOutputLanguageStatusBarItem.command = "ilspy.selectOutputLanguage";
  updateSelectOutputLanguageStatusBarItem(contentProvider);

  const eventHandlerDisposable = vscode.window.onDidChangeActiveTextEditor(() =>
    updateSelectOutputLanguageStatusBarItem(contentProvider)
  );

  return [selectOutputLanguageStatusBarItem, eventHandlerDisposable];
}

function updateSelectOutputLanguageStatusBarItem(
  contentProvider: DecompilerTextDocumentContentProvider
) {
  const document = vscode.window.activeTextEditor?.document;
  if (document !== undefined && document.uri.scheme === ILSPY_URI_SCHEME) {
    const languageInfo =
      languageInfos[contentProvider.getDocumentOutputLanguage(document?.uri)];
    if (languageInfo !== undefined) {
      selectOutputLanguageStatusBarItem.text = `Output: ${languageInfo.displayName}`;
      selectOutputLanguageStatusBarItem.tooltip =
        "Click to select a different language";
      selectOutputLanguageStatusBarItem.show();
    }
    return;
  }

  selectOutputLanguageStatusBarItem.hide();
}
