import { LanguageName } from "../protocol/LanguageName";

const ILSPY_CODE_CS_LANG = "ilspy-code-cs";
const ILSPY_CODE_IL_LANG = "ilspy-code-il";

export interface LanguageInfo {
  name: LanguageName;
  displayName: string;
  vsLanguageMode: string;
}

export const languageInfos = createLanguageMap([
  {
    name: LanguageName.IL,
    displayName: "IL",
    vsLanguageMode: ILSPY_CODE_IL_LANG,
  },
  {
    name: LanguageName.CSharp_1,
    displayName: "C# 1.0 / VS .NET",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_2,
    displayName: "C# 2.0 / VS 2005",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_3,
    displayName: "C# 3.0 / VS 2008",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_4,
    displayName: "C# 4.0 / VS 2010",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_5,
    displayName: "C# 5.0 / VS 2012",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_6,
    displayName: "C# 6.0 / VS 2015",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_7,
    displayName: "C# 7.0 / VS 2017",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_7_1,
    displayName: "C# 7.1 / VS 2017.3",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_7_2,
    displayName: "C# 7.2 / VS 2017.4",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_7_3,
    displayName: "C# 7.3 / VS 2017.7",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_8,
    displayName: "C# 8.0 / VS 2019",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_9,
    displayName: "C# 9.0 / VS 2019.8",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_10,
    displayName: "C# 10.0 / VS 2022",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_11,
    displayName: "C# 11.0 / VS 2022.4",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_12,
    displayName: "C# 12.0 / VS 2022.8",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_13,
    displayName: "C# 13.0 / VS 2022.12",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
  {
    name: LanguageName.CSharp_14,
    displayName: "C# 14.0 / VS 2026",
    vsLanguageMode: ILSPY_CODE_CS_LANG,
  },
]);

export const LATEST_OUTPUT_LANGUAGE: LanguageName = LanguageName.CSharp_14;

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