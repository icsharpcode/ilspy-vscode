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

type DotnetRuntimeSetting = { version: string; path: string };

export function getCachedDotnetRuntimePath(
  extensionContext: vscode.ExtensionContext,
  expectedVersion: string
) {
  const dotnetRuntimeSetting =
    extensionContext.globalState.get<DotnetRuntimeSetting>("dotnetRuntime");
  if (dotnetRuntimeSetting?.version === expectedVersion) {
    return dotnetRuntimeSetting.path;
  }

  return undefined;
}

export function cacheDotnetRuntimePath(
  extensionContext: vscode.ExtensionContext,
  version: string,
  path: string
) {
  extensionContext.globalState.update("dotnetRuntime", {
    version,
    path,
  });
}