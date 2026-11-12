/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { describe, it, expect } from "vitest";
import { languageInfos } from "./languageInfos";
import { LanguageName } from "../extension-types";

describe("languageInfos", () => {
  it("maps IL to the .il file extension", () => {
    expect(languageInfos[LanguageName.IL].fileExtension).toBe(".il");
  });

  it("maps all C# language versions to the .cs file extension", () => {
    const cSharpLanguages = Object.entries(languageInfos).filter(
      ([name]) => name !== LanguageName.IL,
    );
    expect(cSharpLanguages.length).toBeGreaterThan(0);
    for (const [, languageInfo] of cSharpLanguages) {
      expect(languageInfo.fileExtension).toBe(".cs");
    }
  });
});
