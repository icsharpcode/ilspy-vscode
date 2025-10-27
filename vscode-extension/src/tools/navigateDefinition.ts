import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import NodeMetadata from "../protocol/NodeMetadata";
import { parseILSpyUri } from "./utils";

export interface GetDefinitionToolInput {
  documentUri: string;
  line: number;
  character: number;
}

/**
 * Tool: Get Definition
 * Get the definition source code from a position in code
 */
export function registerGetDefinitionTool(
  backend: IILSpyBackend
): vscode.Disposable {
  return vscode.lm.registerTool<GetDefinitionToolInput>("ilspy_getDefinition", {
    async prepareInvocation(
      options,
      _token
    ) {
      const { documentUri, line, character } = options.input;
      return {
        invocationMessage: new vscode.MarkdownString(`$(search-view-icon) Getting definition at \`${documentUri}:${line}:${character}\``),
      };
    },

    async invoke(
      options,
      _token
    ) {
      const { documentUri, line, character } = options.input;

      try {
        const uri = vscode.Uri.parse(documentUri);
        const position = new vscode.Position(line, character);

        // Execute "Go to Definition"
        const definitions = await vscode.commands.executeCommand<vscode.Location[]>(
          "vscode.executeDefinitionProvider",
          uri,
          position
        );

        if (!definitions || definitions.length === 0) {
          throw new Error(`No definition found at ${documentUri}:${line}:${character}`);
        }

        const definition = definitions[0];

        // If it's an ILSpy URI, decompile it
        if (definition.uri.scheme === "ilspy") {
          const nodeMetadata = parseILSpyUri(definition.uri.toString());
          if (nodeMetadata) {
            const response = await backend.sendDecompileNode({
              nodeMetadata,
              outputLanguage: "C# 12.0 / VS 2022.8",
            });

            if (response && !response.isError && response.decompiledCode) {
              return new vscode.LanguageModelToolResult([
                new vscode.LanguageModelTextPart(
                  `${nodeMetadata.name}\n` +
                  `Assembly: ${nodeMetadata.assemblyPath}\n\n` +
                  `\`\`\`csharp\n${response.decompiledCode}\n\`\`\``
                ),
              ]);
            }
          }
        }

        // Otherwise, open the document and return its content
        const document = await vscode.workspace.openTextDocument(definition.uri);
        const sourceCode = document.getText();

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(
            `Location: ${definition.uri.toString()}\n\n` +
            `\`\`\`csharp\n${sourceCode}\n\`\`\``
          ),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(`Get definition failed: ${errorMessage}`);
      }
    }
  });
}