import * as vscode from "vscode";
import { languageFromDisplayName, LanguageInfo } from "./languageInfos";
import { LanguageName } from "../protocol/DecompileResponse";

export function getDefaultOutputLanguage() {
  return (languageFromDisplayName(
    vscode.workspace.getConfiguration("ilspy").get("defaultOutputLanguage")
  ) ?? LanguageName.CSharp) as LanguageName;
}

export function setDefaultOutputLanguage(languageInfo: LanguageInfo) {
  vscode.workspace
    .getConfiguration("ilspy")
    .update(
      "defaultOutputLanguage",
      languageInfo.displayName,
      vscode.ConfigurationTarget.Global
    );
}