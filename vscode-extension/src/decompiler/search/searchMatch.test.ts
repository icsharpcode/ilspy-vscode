import { describe, expect, it } from "vitest";
import { findSearchMatch } from "./searchMatch";

const code = [
  "private void MoveNext()",
  "{",
  "    context.Response.StatusCode = 401;",
  '    string key = "X-Api-Key";',
  "}",
].join("\r\n");

describe("findSearchMatch", () => {
  it("uses the match location if the matched text is there", () => {
    expect(
      findSearchMatch(code, {
        line: 3,
        column: 18,
        length: 9,
        text: "x-api-key",
      }),
    ).toEqual({ line: 3, column: 18, length: 9 });
  });

  it("finds the matched text elsewhere if the location is outdated", () => {
    // E.g. located in IL code, while the document now shows C#.
    expect(
      findSearchMatch(code, { line: 10, column: 5, length: 3, text: "401" }),
    ).toEqual({ line: 2, column: 34, length: 3 });
  });

  it("returns undefined if the matched text is not in the code", () => {
    expect(
      findSearchMatch(code, { line: 0, column: 0, length: 4, text: "nope" }),
    ).toBeUndefined();
  });
});
