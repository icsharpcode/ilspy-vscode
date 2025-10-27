import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import { languageInfos } from "../decompiler/languageInfos";
import { nodeDataToUri } from "../decompiler/nodeUri";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import { filterNodesBySymbolType } from "./utils";
import { NodeType } from "../protocol/NodeType";
import Node from "../protocol/Node";

export interface SearchAndOpenToolInput {
  searchTerm: string;
  symbolType?: "type" | "method" | "field" | "property" | "event" | "all";
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
  return vscode.lm.registerTool<SearchAndOpenToolInput>("ilspy_search_and_open", {
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
      const { searchTerm, symbolType } = options.input;

      try {
        const response = await backend.sendSearch({ term: searchTerm });

        if (!response || !response.results || response.results.length === 0) {
          return new vscode.LanguageModelToolResult([
            new vscode.LanguageModelTextPart(
              `No results found for: "${searchTerm}". Ensure assemblies are loaded with ilspy_load_assembly.`
            ),
          ]);
        }

        // Filter by symbol type if specified
        const filteredResults = filterNodesBySymbolType(response.results, symbolType);

        if (filteredResults.length === 0) {
          return new vscode.LanguageModelToolResult([
            new vscode.LanguageModelTextPart(
              `No ${symbolType || 'symbol'} results found for: "${searchTerm}".`
            ),
          ]);
        }

        /**
         * Determines if a node is a type definition (class, interface, struct, enum, delegate)
         */
        function isTypeNode(node: Node): boolean {
          if (!node.metadata) {
            return false;
          }

          return [
            NodeType.Class,
            NodeType.Interface,
            NodeType.Struct,
            NodeType.Enum,
            NodeType.Delegate,
          ].includes(node.metadata.type);
        }

        /**
         * Sorts nodes by relevance: exact matches first, then types, then members
         */
        function sortByRelevance(searchTerm: string) {
          return (a: Node, b: Node): number => {
            const aExact = a.displayName.toLowerCase() === searchTerm.toLowerCase();
            const bExact = b.displayName.toLowerCase() === searchTerm.toLowerCase();
            if (aExact !== bExact) {
              return aExact ? -1 : 1;
            }

            const aIsType = isTypeNode(a);
            const bIsType = isTypeNode(b);
            if (aIsType !== bIsType) {
              return aIsType ? -1 : 1;
            }

            return 0;
          };
        }

        // Find most relevant match (prefer types over nested members)
        const sortedResults = filteredResults
          .filter((node) => node.metadata)
          .sort(sortByRelevance(searchTerm));

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

        const otherMatchesNote = filteredResults.length > 1
          ? ` (${filteredResults.length - 1} other matches available)`
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