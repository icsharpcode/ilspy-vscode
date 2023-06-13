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

export function getLoadPreviousAssemblies() {
  return (
    vscode.workspace
      .getConfiguration("ilspy")
      .get<boolean>("loadPreviousAssemblies") ?? true
  );
}

export function getAssemblyList(extensionContext: vscode.ExtensionContext) {
  if (getLoadPreviousAssemblies()) {
    return extensionContext.workspaceState.get<string[]>("assemblies") ?? [];
  }

  return [];
}

export function updateAssemblyListIfNeeded(
  extensionContext: vscode.ExtensionContext,
  assemblies: string[]
) {
  if (getLoadPreviousAssemblies()) {
    extensionContext.workspaceState.update("assemblies", assemblies);
  }
}