import * as vscode from "vscode";
import { AnalyzeResultTreeProvider } from "../decompiler/analyze/AnalyzeResultTreeProvider";
import Node from "../protocol/Node";
import { parseILSpyUri } from "./utils";

export interface OpenAnalyzePanelToolInput {
  uri: string;
}

/**
 * Tool: Open Analyze Panel
 * Opens the ILSpy Analyze panel for a given symbol
 * This shows "Used By", "Instantiated By", etc. for the symbol
 */
export function registerOpenAnalyzePanelTool(
  analyzeResultTreeProvider: AnalyzeResultTreeProvider
): vscode.Disposable {
  return vscode.lm.registerTool<OpenAnalyzePanelToolInput>("ilspy_openAnalyzePanel", {
    async prepareInvocation(
      options,
      _token
    ) {
      const { uri } = options.input;
      return {
        invocationMessage: new vscode.MarkdownString(`$(graph) Opening analyze panel: \`${uri}\``),
        confirmationMessages: {
          title: "Open Analyze Panel",
          message: new vscode.MarkdownString(
            `Open ILSpy Analyze panel to show usage, references, and relationships:\n\n**URI:** \`${uri}\``
          ),
        },
      };
    },

    async invoke(
      options,
      _token
    ) {
      const { uri } = options.input;

      try {
        const nodeMetadata = parseILSpyUri(uri);
        if (!nodeMetadata) {
          throw new Error(`Invalid ILSpy URI: ${uri}`);
        }

        // Create a Node object from the URI
        const node: Node = {
          displayName: nodeMetadata.name,
          description: "",
          metadata: nodeMetadata,
          mayHaveChildren: true,
          modifiers: 0,
          flags: 0
        };

        // Trigger analyze panel
        vscode.commands.executeCommand(
          "setContext",
          "ilspy.analyzeResultsToShow",
          true
        );
        await analyzeResultTreeProvider.analyze(node);
        vscode.commands.executeCommand("ilspyAnalyzeResultsContainer.focus");

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(
            `Opened analyze panel for ${nodeMetadata.name}`
          ),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(`Failed to open analyze panel: ${errorMessage}`);
      }
    }
  });
}