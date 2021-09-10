import * as vscode from "vscode";
import { languageFromDisplayName } from "./languageInfos";
import { LanguageName } from "../protocol/DecompileResponse";

export function getDefaultOutputLanguage() {
  return (languageFromDisplayName(
    vscode.workspace.getConfiguration("ilspy").get("defaultOutputLanguage")
  ) ?? LanguageName.CSharp) as LanguageName;
}
