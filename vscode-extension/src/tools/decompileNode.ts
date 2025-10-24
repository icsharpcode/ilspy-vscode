import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { parseILSpyUri } from "./utils";

export interface DecompileNodeToolInput {
  uri: string;
  outputLanguage?: string;
}

/**
 * Tool: Decompile Node
 * Decompile a specific type, method, property, etc. by URI
 */
export function registerDecompileNodeTool(
  backend: IILSpyBackend
): vscode.Disposable {
  return vscode.lm.registerTool<DecompileNodeToolInput>("ilspy_decompileNode", {
    async prepareInvocation(
      options,
      _token
    ) {
      const { uri, outputLanguage = "C# 12.0 / VS 2022.8" } = options.input;
      return {
        invocationMessage: new vscode.MarkdownString(`$(code) Decompiling: \`${uri}\``),
      };
    },

    async invoke(
      options,
      _token
    ) {
      const { uri, outputLanguage = "C# 12.0 / VS 2022.8" } = options.input;

      try {
        // Parse ILSpy URI to extract node metadata
        const nodeMetadata = parseILSpyUri(uri);
        if (!nodeMetadata) {
          throw new Error(`Invalid ILSpy URI: ${uri}`);
        }

        const response = await backend.sendDecompileNode({
          nodeMetadata,
          outputLanguage,
        });

        if (!response || response.isError || !response.decompiledCode) {
          throw new Error(response?.errorMessage || "Decompilation failed");
        }

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(
            `${nodeMetadata.name}\n` +
            `Assembly: ${nodeMetadata.assemblyPath}\n\n` +
            `\`\`\`csharp\n${response.decompiledCode}\n\`\`\``
          ),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(`Decompilation failed: ${errorMessage}`);
      }
    }
  });
}