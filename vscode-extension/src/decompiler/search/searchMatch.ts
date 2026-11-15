/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { SearchMatch } from "../../extension-types";

/**
 * Finds the position of a search match in decompiled code. Uses the match's line and column if the
 * matched text is found there, otherwise (e.g. if the output language changed since searching) the
 * first occurrence of the matched text.
 */
export function findSearchMatch(
  code: string,
  match: SearchMatch,
): { line: number; column: number; length: number } | undefined {
  const lines = code.split("\n");
  const text = match.text.toLowerCase();
  const lineText = lines[match.line]?.replace(/\r$/, "");
  if (
    lineText
      ?.substring(match.column, match.column + match.length)
      .toLowerCase() === text
  ) {
    return { line: match.line, column: match.column, length: match.length };
  }

  for (let line = 0; line < lines.length; line++) {
    const column = lines[line].toLowerCase().indexOf(text);
    if (column >= 0) {
      return { line, column, length: match.text.length };
    }
  }

  return undefined;
}
