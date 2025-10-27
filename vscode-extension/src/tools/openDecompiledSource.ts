import * as vscode from "vscode";
import { DecompilerTextDocumentContentProvider } from "../decompiler/DecompilerTextDocumentContentProvider";
import { languageInfos } from "../decompiler/languageInfos";
import { getDefaultOutputLanguage } from "../decompiler/settings";
import { parseILSpyUri } from "./utils";

export interface OpenToolInput {
  uri: string;
}

/**
 * Tool: Open
 * Opens a decompiled symbol in a VS Code editor tab
 * This is a UI action tool - it directly opens the editor, not just returning text
 */
export function registerOpenTool(
  contentProvider: DecompilerTextDocumentContentProvider
): vscode.Disposable {
  return vscode.lm.registerTool<OpenToolInput>("ilspy_open", {
    async prepareInvocation(
      options,
      _token
    ) {
      const { uri } = options.input;
      return {
        invocationMessage: new vscode.MarkdownString(`$(go-to-file) Opening decompiled source: \`${uri}\``),
        confirmationMessages: {
          title: "Open Decompiled Source",
          message: new vscode.MarkdownString(
            `Open decompiled source in editor:\n\n**URI:** \`${uri}\``
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
        const parsedUri = vscode.Uri.parse(uri);
        if (parsedUri.scheme !== "ilspy") {
          throw new Error(`Invalid ILSpy URI: ${uri}`);
        }

        const nodeMetadata = parseILSpyUri(uri);
        if (!nodeMetadata) {
          throw new Error(`Could not parse ILSpy URI: ${uri}`);
        }

        const language = getDefaultOutputLanguage();
        contentProvider.setDocumentOutputLanguage(parsedUri, language);

        let doc = await vscode.workspace.openTextDocument(parsedUri);
        vscode.languages.setTextDocumentLanguage(
          doc,
          languageInfos[language].vsLanguageMode
        );
        await vscode.window.showTextDocument(doc, { preview: false });

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(
            `Opened ${nodeMetadata.name} in editor`
          ),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(`Failed to open source: ${errorMessage}`);
      }
    }
  });
}