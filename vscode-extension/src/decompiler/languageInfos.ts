import { LanguageName } from "../protocol/DecompileResponse";

export interface LanguageInfo {
  displayName: string;
  vsLanguageMode: string;
}

export const languageInfos: { [key: string]: LanguageInfo } = {
  [LanguageName.CSharp]: { displayName: "C#", vsLanguageMode: "csharp" },
  [LanguageName.IL]: { displayName: "IL", vsLanguageMode: "il" },
};

export const languageFromDisplayName = (name?: string) =>
  Object.entries(languageInfos).find(
    (entry) => entry[1].displayName === name
  )?.[0] as LanguageName | undefined;
