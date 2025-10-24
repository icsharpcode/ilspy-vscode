import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { nodeDataToUri } from "../decompiler/nodeUri";

export interface SearchAssembliesToolInput {
  searchTerm: string;
}

/**
 * Tool: Search Assemblies
 * Search for types, methods, properties, etc. across loaded assemblies
 */
export function registerSearchAssembliesTool(
  backend: IILSpyBackend
): vscode.Disposable {
  return vscode.lm.registerTool<SearchAssembliesToolInput>("ilspy_searchAssemblies", {
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

        // Return structured data - let the LLM format it
        const results = response.results
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
                totalResults: response.results.length,
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