/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as path from "path";
import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import {
  getDefaultOutputLanguage,
  getShowCompilerGeneratedSymbolsSetting,
} from "../decompiler/settings";
import { hasNodeFlag, isTypeNode } from "../decompiler/utils";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { LanguageName } from "../protocol/LanguageName";
import Node from "../protocol/Node";
import { NodeFlags } from "../protocol/NodeFlags";
import { NodeType } from "../protocol/NodeType";

class ExportCancelledError extends Error {}

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
      const fileExtension = outputLanguage === LanguageName.IL ? ".il" : ".cs";

      const assemblyFolderName = getAssemblyFolderName(assemblyNode);
      const assemblyOutputDir = await createUniqueDirectory(
        vscode.Uri.joinPath(baseOutputDir, assemblyFolderName)
      );

      try {
        await vscode.window.withProgress(
          {
            location: vscode.ProgressLocation.Notification,
            title: `ILSpy: Export decompiled code`,
            cancellable: true,
          },
          async (progress, token) => {
            const errors: string[] = [];

            progress.report({ message: "Collecting types..." });
            const namespaces = (await decompiledTreeProvider.getChildNodes(
              assemblyNode
            )).filter((n) => n.metadata?.type === NodeType.Namespace);

            const exportItems: ExportItem[] = [];
            const usedOutputPaths = new Set<string>();
            const includeCompilerGenerated =
              getShowCompilerGeneratedSymbolsSetting();

            for (const nsNode of namespaces) {
              throwIfCancelled(token);

              const nsName = nsNode.metadata?.name ?? "";
              const nsDir = getNamespaceDirectory(assemblyOutputDir, nsName);

              const typeNodes = (await decompiledTreeProvider.getChildNodes(
                nsNode
              ))
                .filter((n) => isTypeNode(n.metadata?.type ?? NodeType.Unknown))
                .filter(
                  (n) =>
                    includeCompilerGenerated ||
                    !hasNodeFlag(n, NodeFlags.CompilerGenerated)
                );

              for (const typeNode of typeNodes) {
                const symbolToken = typeNode.metadata?.symbolToken ?? 0;
                const fileNameBase = sanitizeFileBaseName(
                  typeNode.metadata?.name ?? `type_${symbolToken}`
                );

                const baseFileName = `${fileNameBase}${fileExtension}`;
                const baseUri = vscode.Uri.joinPath(nsDir, baseFileName);
                const outputUri = usedOutputPaths.has(baseUri.toString())
                  ? vscode.Uri.joinPath(
                      nsDir,
                      `${fileNameBase}_${symbolToken}${fileExtension}`
                    )
                  : baseUri;
                usedOutputPaths.add(outputUri.toString());

                exportItems.push({
                  node: typeNode,
                  outputFile: outputUri,
                  outputDir: nsDir,
                });
              }
            }

            const totalFiles = exportItems.length + 1;
            let completedFiles = 0;
            const reportFileDone = (message?: string) => {
              completedFiles++;
              progress.report({
                message,
                increment: (1 / totalFiles) * 100,
              });
            };

            throwIfCancelled(token);
            progress.report({ message: "Writing AssemblyInfo..." });

            const assemblyInfo = await backend.sendDecompileNode({
              nodeMetadata: assemblyMetadata,
              outputLanguage,
            });

            if (assemblyInfo?.shouldUpdateAssemblyList) {
              vscode.commands.executeCommand("ilspy.refreshAssemblyList");
            }

            await vscode.workspace.fs.writeFile(
              vscode.Uri.joinPath(
                assemblyOutputDir,
                `AssemblyInfo${fileExtension}`
              ),
              Buffer.from(
                ensureTrailingNewline(
                  assemblyInfo?.decompiledCode ??
                    `// Failed to decompile assembly.\n// ${
                      assemblyInfo?.errorMessage ?? ""
                    }\n`
                ),
                "utf8"
              )
            );
            if (assemblyInfo?.isError) {
              errors.push(
                `AssemblyInfo: ${assemblyInfo.errorMessage ?? "unknown error"}`
              );
            }
            reportFileDone();

            const createdDirs = new Set<string>();
            for (const item of exportItems) {
              throwIfCancelled(token);

              const dirKey = item.outputDir.toString();
              if (!createdDirs.has(dirKey)) {
                await vscode.workspace.fs.createDirectory(item.outputDir);
                createdDirs.add(dirKey);
              }

              const response = await backend.sendDecompileNode({
                nodeMetadata: item.node.metadata!,
                outputLanguage,
              });

              if (response?.shouldUpdateAssemblyList) {
                vscode.commands.executeCommand("ilspy.refreshAssemblyList");
              }

              const code =
                response?.decompiledCode ??
                `// Failed to decompile.\n// ${response?.errorMessage ?? ""}\n`;
              await vscode.workspace.fs.writeFile(
                item.outputFile,
                Buffer.from(ensureTrailingNewline(code), "utf8")
              );

              if (response?.isError) {
                errors.push(
                  `${item.node.metadata?.name ?? "type"}: ${
                    response.errorMessage ?? "unknown error"
                  }`
                );
              }

              reportFileDone(
                `Exported ${completedFiles}/${totalFiles}: ${
                  item.node.metadata?.name ?? ""
                }`
              );
            }

            if (errors.length > 0) {
              vscode.window.showWarningMessage(
                `Export completed with ${errors.length} errors. Output: ${assemblyOutputDir.fsPath}`
              );
            } else {
              const choice = await vscode.window.showInformationMessage(
                `Exported ${totalFiles} files to ${assemblyOutputDir.fsPath}`,
                "Reveal in File Explorer"
              );
              if (choice === "Reveal in File Explorer") {
                vscode.commands.executeCommand(
                  "revealFileInOS",
                  assemblyOutputDir
                );
              }
            }
          }
        );
      } catch (e) {
        if (e instanceof ExportCancelledError) {
          return;
        }
        throw e;
      }
    }
  );
}

type ExportItem = {
  node: Node;
  outputFile: vscode.Uri;
  outputDir: vscode.Uri;
};

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

function getAssemblyFolderName(assemblyNode: Node) {
  const rawName =
    assemblyNode.metadata?.name ??
    path.basename(assemblyNode.metadata?.assemblyPath ?? "assembly");
  return sanitizePathSegment(path.parse(rawName).name || rawName);
}

function getNamespaceDirectory(baseDir: vscode.Uri, namespaceName: string) {
  if (!namespaceName) {
    return baseDir;
  }
  const parts = namespaceName.split(".").filter(Boolean);
  const safeParts = parts.map(sanitizePathSegment).filter(Boolean);
  return safeParts.reduce(
    (uri, part) => vscode.Uri.joinPath(uri, part),
    baseDir
  );
}

async function createUniqueDirectory(dir: vscode.Uri) {
  const base = dir;
  const parent = base.with({ path: path.posix.dirname(base.path) });
  const baseName = path.posix.basename(base.path);
  let candidate = base;
  for (let i = 1; i < 1000; i++) {
    try {
      await vscode.workspace.fs.stat(candidate);
      candidate = vscode.Uri.joinPath(parent, `${baseName}-${i}`);
    } catch {
      await vscode.workspace.fs.createDirectory(candidate);
      return candidate;
    }
  }

  await vscode.workspace.fs.createDirectory(candidate);
  return candidate;
}

function throwIfCancelled(token: vscode.CancellationToken) {
  if (token.isCancellationRequested) {
    throw new ExportCancelledError("Export cancelled");
  }
}

function ensureTrailingNewline(s: string) {
  return s.endsWith("\n") ? s : `${s}\n`;
}

function sanitizePathSegment(segment: string) {
  const trimmed = segment.trim();
  const sanitized = trimmed
    .replace(/[<>:"/\\\\|?*]/g, "_")
    .replace(/[\u0000-\u001F\u007F]/g, "_")
    .replace(/[. ]+$/g, "");

  const safe = sanitized.length > 0 ? sanitized : "_";
  return isWindowsReservedName(safe) ? `_${safe}` : safe;
}

function sanitizeFileBaseName(name: string) {
  return sanitizePathSegment(name).replace(/\s+/g, " ");
}

function isWindowsReservedName(name: string) {
  const upper = name.toUpperCase();
  if (["CON", "PRN", "AUX", "NUL"].includes(upper)) {
    return true;
  }
  if (/^COM[1-9]$/.test(upper)) {
    return true;
  }
  if (/^LPT[1-9]$/.test(upper)) {
    return true;
  }
  return false;
}
