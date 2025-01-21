import { LanguageName } from "../protocol/LanguageName";

export interface LanguageInfo {
  name: LanguageName;
  displayName: string;
  vsLanguageMode: string;
}

export const languageInfos = createLanguageMap([
  { name: LanguageName.IL, displayName: "IL", vsLanguageMode: "il" },
  {
    name: LanguageName.CSharp_1,
    displayName: "C# 1.0 / VS .NET",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_2,
    displayName: "C# 2.0 / VS 2005",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_3,
    displayName: "C# 3.0 / VS 2008",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_4,
    displayName: "C# 4.0 / VS 2010",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_5,
    displayName: "C# 5.0 / VS 2012",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_6,
    displayName: "C# 6.0 / VS 2015",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_7,
    displayName: "C# 7.0 / VS 2017",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_7_1,
    displayName: "C# 7.1 / VS 2017.3",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_7_2,
    displayName: "C# 7.2 / VS 2017.4",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_7_3,
    displayName: "C# 7.3 / VS 2017.7",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_8,
    displayName: "C# 8.0 / VS 2019",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_9,
    displayName: "C# 9.0 / VS 2019.8",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_10,
    displayName: "C# 10.0 / VS 2022",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_11,
    displayName: "C# 11.0 / VS 2022.4",
    vsLanguageMode: "csharp",
  },
  {
    name: LanguageName.CSharp_12,
    displayName: "C# 12.0 / VS 2022.8",
    vsLanguageMode: "csharp",
  },
]);

export const LATEST_OUTPUT_LANGUAGE: LanguageName = LanguageName.CSharp_12;

export const languageFromDisplayName = (name?: string) =>
  Object.entries(languageInfos).find(
    (entry) => entry[1].displayName === name
  )?.[0] as LanguageName | undefined;

function createLanguageMap(languageInfos: LanguageInfo[]) {
  const languageMap: Record<string, LanguageInfo> = {};
  languageInfos.forEach(
    (languageInfo) => (languageMap[languageInfo.name] = languageInfo)
  );
  return languageMap;
}