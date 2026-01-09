/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import {
  getDefaultOutputLanguage,
  getShowCompilerGeneratedSymbolsSetting,
} from "../decompiler/settings";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";

export function registerExportDecompiledAssemblyCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
  backend: IILSpyBackend
) {
  return vscode.commands.registerCommand(
    "ilspy.exportDecompiledAssembly",
    async (node?: Node) => {
      const assemblyNode = await getOrPickAssemblyNode(
        node,
        decompiledTreeProvider
      );
      if (!assemblyNode?.metadata) {
        return;
      }
      const assemblyMetadata = assemblyNode.metadata;

      const baseOutputDir = await promptForExportDirectory();
      if (!baseOutputDir) {
        return;
      }

      const outputLanguage = getDefaultOutputLanguage();
      const includeCompilerGenerated =
        getShowCompilerGeneratedSymbolsSetting();

      await vscode.window.withProgress(
        {
          location: vscode.ProgressLocation.Notification,
          title: `ILSpy: Export decompiled code`,
          cancellable: true,
        },
        async (progress, token) => {
          progress.report({ message: "Exporting..." });
          try {
            const response = await backend.sendExportAssembly(
              {
                nodeMetadata: assemblyMetadata,
                outputLanguage,
                outputDirectory: baseOutputDir.fsPath,
                includeCompilerGenerated,
              },
              token
            );

            if (response?.shouldUpdateAssemblyList) {
              vscode.commands.executeCommand("ilspy.refreshAssemblyList");
            }

            if (!response) {
              vscode.window.showErrorMessage("Export failed.");
              return;
            }

            if (!response.succeeded) {
              vscode.window.showErrorMessage(
                response.errorMessage
                  ? `Export failed: ${response.errorMessage}`
                  : "Export failed."
              );
              return;
            }

            const outputDirectory =
              response.outputDirectory ?? baseOutputDir.fsPath;
            const outputUri = vscode.Uri.file(outputDirectory);
            const fileCount = response.filesWritten ?? 0;

            if (response.errorCount > 0) {
              vscode.window.showWarningMessage(
                `Exported ${fileCount} files to ${outputDirectory} with ${response.errorCount} errors.`,
                "Reveal in File Explorer"
              ).then((choice) => {
                if (choice === "Reveal in File Explorer") {
                  vscode.commands.executeCommand("revealFileInOS", outputUri);
                }
              });
              return;
            }

            vscode.window.showInformationMessage(
              `Exported ${fileCount} files to ${outputDirectory}`,
              "Reveal in File Explorer"
            ).then((choice) => {
              if (choice === "Reveal in File Explorer") {
                vscode.commands.executeCommand("revealFileInOS", outputUri);
              }
            });
          } catch (err) {
            if (token.isCancellationRequested) {
              return;
            }
            const message =
              err instanceof Error ? err.message : String(err);
            vscode.window.showErrorMessage(`Export failed: ${message}`);
          }
        }
      );
    }
  );
}

async function getOrPickAssemblyNode(
  node: Node | undefined,
  decompiledTreeProvider: DecompiledTreeProvider
) {
  if (node?.metadata?.type === NodeType.Assembly) {
    return node;
  }

  const assemblies = (await decompiledTreeProvider.getChildNodes()).filter(
    (n) => n.metadata?.type === NodeType.Assembly
  );
  if (assemblies.length === 0) {
    vscode.window.showWarningMessage("No assemblies loaded.");
    return undefined;
  }

  const pick = await vscode.window.showQuickPick(
    assemblies.map((a) => ({
      label: a.displayName,
      description: a.description,
      node: a,
    })),
    { title: "Select assembly to export" }
  );

  return pick?.node;
}

async function promptForExportDirectory(): Promise<vscode.Uri | undefined> {
  const dirs = await vscode.window.showOpenDialog({
    openLabel: "Export here",
    canSelectFiles: false,
    canSelectFolders: true,
    canSelectMany: false,
  });
  return dirs?.[0];
}
