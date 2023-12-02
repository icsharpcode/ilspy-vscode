import * as vscode from "vscode";
import {
  DEFAULT_OUTPUT_LANGUAGE,
  languageFromDisplayName,
  LanguageInfo,
} from "./languageInfos";
import { LanguageName } from "../protocol/LanguageName";

export function getDefaultOutputLanguage() {
  return (languageFromDisplayName(
    vscode.workspace.getConfiguration("ilspy").get("defaultOutputLanguage")
  ) ?? DEFAULT_OUTPUT_LANGUAGE) as LanguageName;
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