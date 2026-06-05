/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { executeILSpyCommand } from "../commands/commandUtils";
import { NodeType } from "../extension-types";
import IILSpyBackend from "./IILSpyBackend";
import { ILSPY_URI_SCHEME, uriToNode } from "./nodeUri";

const OUTLINE_NODE_TYPES: Partial<Record<NodeType, vscode.SymbolKind>> = {
  [NodeType.Assembly]: vscode.SymbolKind.Module,
  [NodeType.Namespace]: vscode.SymbolKind.Namespace,
  [NodeType.Class]: vscode.SymbolKind.Class,
  [NodeType.Interface]: vscode.SymbolKind.Interface,
  [NodeType.Struct]: vscode.SymbolKind.Struct,
  [NodeType.Enum]: vscode.SymbolKind.Enum,
  [NodeType.Delegate]: vscode.SymbolKind.Function,
  [NodeType.Event]: vscode.SymbolKind.Event,
  [NodeType.Field]: vscode.SymbolKind.Field,
  [NodeType.Method]: vscode.SymbolKind.Method,
  [NodeType.Const]: vscode.SymbolKind.Constant,
  [NodeType.Property]: vscode.SymbolKind.Property,
};

export class DecompiledDocumentSymbolProvider
  implements vscode.DocumentSymbolProvider
{
  constructor(private backend: IILSpyBackend) {}

  async provideDocumentSymbols(
    document: vscode.TextDocument,
    token: vscode.CancellationToken,
  ): Promise<vscode.DocumentSymbol[]> {
    if (document.uri.scheme !== ILSPY_URI_SCHEME) {
      return [];
    }

    const nodeMetadata = uriToNode(document.uri);
    if (
      !nodeMetadata ||
      token.isCancellationRequested ||
      !(nodeMetadata.type in OUTLINE_NODE_TYPES)
    ) {
      return [];
    }

    const response = await this.backend.sendGetNodes({ nodeMetadata });
    if (token.isCancellationRequested) {
      return [];
    }

    if (response?.shouldUpdateAssemblyList) {
      await executeILSpyCommand("ilspy.refreshAssemblyList");
    }

    const symbolAnchor = document.positionAt(0);
    const symbolRange = new vscode.Range(symbolAnchor, symbolAnchor);

    return (response?.nodes ?? [])
      .filter((node) => {
        const nodeType = node.metadata?.type;
        return nodeType !== undefined && nodeType in OUTLINE_NODE_TYPES;
      })
      .map(
        (node) =>
          new vscode.DocumentSymbol(
            node.displayName || node.metadata?.name || node.description,
            node.description,
            OUTLINE_NODE_TYPES[node.metadata?.type ?? NodeType.Unknown] ??
              vscode.SymbolKind.Object,
            symbolRange,
            symbolRange,
          ),
      );
  }
}

export function registerDecompiledDocumentSymbolProvider(
  ilspyBackend: IILSpyBackend,
) {
  return vscode.languages.registerDocumentSymbolProvider(
    { scheme: ILSPY_URI_SCHEME },
    new DecompiledDocumentSymbolProvider(ilspyBackend),
  );
}
