import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import { languageInfos } from "../decompiler/languageInfos";
import { nodeDataToUri } from "../decompiler/nodeUri";
import { getDefaultOutputLanguage } from "../decompiler/settings";

export interface SearchAndOpenToolInput {
  searchTerm: string;
}

/**
 * Tool: Search and Open
 * Combines search + open workflow - searches for a symbol and opens the most relevant match
 * This is a convenience tool for "show me X" type queries
 */
export function registerSearchAndOpenTool(
  backend: IILSpyBackend,
  contentProvider: DecompilerTextDocumentContentProvider
): vscode.Disposable {
  return vscode.lm.registerTool<SearchAndOpenToolInput>("ilspy_searchAndOpen", {
    async prepareInvocation(
      options,
      _token
    ) {
      const { searchTerm } = options.input;
      return {
        invocationMessage: new vscode.MarkdownString(`$(search) Searching and opening: \`${searchTerm}\``),
        confirmationMessages: {
          title: "Search and Open Decompiled Source",
          message: new vscode.MarkdownString(
            `Search for a symbol and open the most relevant match in the editor:\n\n**Search:** \`${searchTerm}\``
          ),
        },
      };
    },

    async invoke(
      options,
      _token
    ) {
      const { searchTerm } = options.input;

      try {
        const response = await backend.sendSearch({ term: searchTerm });

        if (!response || !response.results || response.results.length === 0) {
          return new vscode.LanguageModelToolResult([
            new vscode.LanguageModelTextPart(
              `No results found for: "${searchTerm}". Ensure assemblies are loaded with ilspy_addAssembly.`
            ),
          ]);
        }

        // Find most relevant match (prefer types over nested members)
        const sortedResults = response.results
          .filter((node) => node.metadata)
          .sort((a, b) => {
            // Prefer exact matches
            const aExact = a.displayName.toLowerCase() === searchTerm.toLowerCase();
            const bExact = b.displayName.toLowerCase() === searchTerm.toLowerCase();
            if (aExact && !bExact) return -1;
            if (!aExact && bExact) return 1;

            // Prefer types (classes, interfaces, structs, enums) over members
            const aIsType = a.description?.includes("class") || a.description?.includes("interface") ||
              a.description?.includes("struct") || a.description?.includes("enum");
            const bIsType = b.description?.includes("class") || b.description?.includes("interface") ||
              b.description?.includes("struct") || b.description?.includes("enum");
            if (aIsType && !bIsType) return -1;
            if (!aIsType && bIsType) return 1;

            return 0;
          });

        const bestMatch = sortedResults[0];
        const uri = nodeDataToUri(bestMatch);

        // Open the document
        const language = getDefaultOutputLanguage();
        contentProvider.setDocumentOutputLanguage(uri, language);

        let doc = await vscode.workspace.openTextDocument(uri);
        vscode.languages.setTextDocumentLanguage(
          doc,
          languageInfos[language].vsLanguageMode
        );
        await vscode.window.showTextDocument(doc, { preview: false });

        const otherMatchesNote = response.results.length > 1
          ? ` (${response.results.length - 1} other matches available)`
          : "";

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(
            `Opened ${bestMatch.displayName} in editor${otherMatchesNote}`
          ),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(`Search and open failed: ${errorMessage}`);
      }
    }
  });
}