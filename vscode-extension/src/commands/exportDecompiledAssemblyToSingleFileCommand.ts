/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as path from "path";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { languageInfos } from "../decompiler/languageInfos";
import {
  getDefaultOutputLanguage,
  getShowCompilerGeneratedSymbolsSetting,
} from "../decompiler/settings";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import {
  ExportOutputType,
  LanguageName,
  Node,
  NodeMetadata,
} from "../extension-types";
import { executeILSpyCommand, registerILSpyCommand } from "./commandUtils";
import { getOrPickAssemblyNode } from "./utils";

export function registerExportDecompiledAssemblyToSingleFileCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
  backend: IILSpyBackend,
) {
  return registerILSpyCommand("ilspy.exportSingleFile", async (node?: Node) => {
    const assemblyNode = await getOrPickAssemblyNode(
      node,
      decompiledTreeProvider,
    );
    if (!assemblyNode?.metadata) {
      return;
    }
    const assemblyMetadata = assemblyNode.metadata;

    const outputLanguage = getDefaultOutputLanguage();
    const includeCompilerGenerated = getShowCompilerGeneratedSymbolsSetting();

    const targetFile = await promptForExportFile(
      assemblyMetadata,
      outputLanguage,
    );
    if (!targetFile) {
      return;
    }

    await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: `ILSpy: Export decompiled code to single file`,
        cancellable: true,
      },
      async (progress, token) => {
        progress.report({ message: "Exporting..." });
        try {
          // In SingleFile mode, outputTarget carries the target file path.
          const response = await backend.sendExportAssembly(
            {
              nodeMetadata: assemblyMetadata,
              outputLanguage,
              outputTarget: targetFile.fsPath,
              includeCompilerGenerated,
              exportOutputType: ExportOutputType.SingleFile,
            },
            token,
          );

          if (response?.shouldUpdateAssemblyList) {
            await executeILSpyCommand("ilspy.refreshAssemblyList");
          }

          if (!response) {
            vscode.window.showErrorMessage("Export failed.");
            return;
          }

          if (!response.succeeded) {
            vscode.window.showErrorMessage(
              response.errorMessage
                ? `Export failed: ${response.errorMessage}`
                : "Export failed.",
            );
            return;
          }

          // In SingleFile mode, the response's outputTarget is the written file path.
          const outputFile = response.outputTarget ?? targetFile.fsPath;
          const outputUri = vscode.Uri.file(outputFile);

          if (response.errorCount > 0) {
            vscode.window
              .showWarningMessage(
                `Exported to ${outputFile} with ${response.errorCount} errors.`,
                "Open File",
              )
              .then((choice) => {
                if (choice === "Open File") {
                  vscode.commands.executeCommand("vscode.open", outputUri);
                }
              });
            return;
          }

          vscode.window
            .showInformationMessage(`Exported to ${outputFile}`, "Open File")
            .then((choice) => {
              if (choice === "Open File") {
                vscode.commands.executeCommand("vscode.open", outputUri);
              }
            });
        } catch (err) {
          if (token.isCancellationRequested) {
            return;
          }
          const message = err instanceof Error ? err.message : String(err);
          vscode.window.showErrorMessage(`Export failed: ${message}`);
        }
      },
    );
  });
}

async function promptForExportFile(
  assemblyMetadata: NodeMetadata,
  outputLanguage: LanguageName,
): Promise<vscode.Uri | undefined> {
  const isIL = outputLanguage === LanguageName.IL;
  const fileExtension =
    languageInfos[outputLanguage]?.fileExtension ?? (isIL ? ".il" : ".cs");

  const assemblyFileName = path.basename(assemblyMetadata.name);
  const baseName = path.basename(
    assemblyFileName,
    path.extname(assemblyFileName),
  );
  const defaultFileName = `${baseName || "assembly"}${fileExtension}`;
  const defaultUri = vscode.Uri.file(
    path.join(path.dirname(assemblyMetadata.assemblyPath), defaultFileName),
  );

  return vscode.window.showSaveDialog({
    title: "Export decompiled code to single file",
    defaultUri,
    filters: { [isIL ? "IL" : "C#"]: [fileExtension.replace(/^\./, "")] },
    saveLabel: "Export here",
  });
}
