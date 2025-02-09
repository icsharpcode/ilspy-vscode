import * as vscode from "vscode";
import {
  languageFromDisplayName,
  LanguageInfo,
  LATEST_OUTPUT_LANGUAGE,
} from "./languageInfos";
import { LanguageName } from "../protocol/LanguageName";

export function getDefaultOutputLanguage() {
  return (languageFromDisplayName(
    vscode.workspace.getConfiguration("ilspy").get("defaultOutputLanguage")
  ) ?? LATEST_OUTPUT_LANGUAGE) as LanguageName;
}

export function setDefaultOutputLanguage(languageInfo: LanguageInfo) {
  vscode.workspace
    .getConfiguration("ilspy")
    .update(
      "defaultOutputLanguage",
      languageInfo.name !== LATEST_OUTPUT_LANGUAGE
        ? languageInfo.displayName
        : undefined,
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

export function getAutoLoadDependenciesSetting() {
  return (
    vscode.workspace
      .getConfiguration("ilspy")
      .get<boolean>("autoLoadDependencies") ?? true
  );
}

export function setAutoLoadDependenciesSetting(autoLoadDependencies: boolean) {
  vscode.workspace
    .getConfiguration("ilspy")
    .update(
      "autoLoadDependencies",
      autoLoadDependencies,
      vscode.ConfigurationTarget.Global
    );
}

export function getShowCompilerGeneratedSymbolsSetting() {
  return (
    vscode.workspace
      .getConfiguration("ilspy")
      .get<boolean>("showCompilerGeneratedSymbols") ?? true
  );
}

export function setShowCompilerGeneratedSymbolsSetting(
  autoLoadDependencies: boolean
) {
  vscode.workspace
    .getConfiguration("ilspy")
    .update(
      "showCompilerGeneratedSymbols",
      autoLoadDependencies,
      vscode.ConfigurationTarget.Global
    );
}