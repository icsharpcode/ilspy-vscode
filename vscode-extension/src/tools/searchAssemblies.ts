import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { nodeDataToUri } from "../decompiler/nodeUri";
import { filterNodesBySymbolType } from "./utils";

export interface SearchToolInput {
  searchTerm: string;
  symbolType?: "type" | "method" | "field" | "property" | "event" | "all";
}

/**
 * Tool: Search
 * Search for types, methods, properties, etc. across loaded assemblies
 */
export function registerSearchTool(
  backend: IILSpyBackend
): vscode.Disposable {
  return vscode.lm.registerTool<SearchToolInput>("ilspy_search", {
    async prepareInvocation(
      options,
      _token
    ) {
      const { searchTerm } = options.input;
      return {
        invocationMessage: new vscode.MarkdownString(`$(search) Searching for: \`${searchTerm}\``),
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
              `No ${symbolType || "symbol"} results found for: "${searchTerm}".`
            ),
          ]);
        }

        // Return structured data - let the LLM format it
        const results = filteredResults
          .filter((node) => node.metadata)
          .slice(0, 20)
          .map((node, index) => ({
            index: index + 1,
            name: node.displayName,
            type: node.description,
            assembly: node.metadata!.assemblyPath.split('/').pop() || node.metadata!.assemblyPath,
            uri: nodeDataToUri(node).toString(),
          }));

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(
            JSON.stringify(
              {
                searchTerm,
                totalResults: filteredResults.length,
                results,
              },
              null,
              2
            )
          ),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(`Search failed: ${errorMessage}`);
      }
    }
  });
}