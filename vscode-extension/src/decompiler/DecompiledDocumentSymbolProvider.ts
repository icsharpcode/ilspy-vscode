/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { executeILSpyCommand } from "../commands/commandUtils";
import { LanguageName, NodeType } from "../extension-types";
import IILSpyBackend from "./IILSpyBackend";
import { ILSPY_URI_SCHEME, uriToNode } from "./nodeUri";
import { NodePosition } from "../protocol/getNodePositions";
import { getDefaultOutputLanguage } from "./settings";

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
  constructor(
    private backend: IILSpyBackend,
    private getOutputLanguage: (uri: vscode.Uri) => LanguageName = () =>
      getDefaultOutputLanguage(),
  ) {}

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

    const positionsResponse = await this.backend.sendGetNodePositions({
      nodeMetadata,
      outputLanguage: this.getOutputLanguage(document.uri),
    });
    if (token.isCancellationRequested) {
      return [];
    }

    if (positionsResponse?.shouldUpdateAssemblyList) {
      await executeILSpyCommand("ilspy.refreshAssemblyList");
    }

    const positionsByToken = new Map(
      (positionsResponse?.positions ?? []).map((position) => [
        position.symbolToken,
        position,
      ]),
    );

    const symbolAnchor = document.positionAt(0);
    const symbolRange = new vscode.Range(symbolAnchor, symbolAnchor);

    const toRange = (position: NodePosition) =>
      new vscode.Range(
        position.startLine,
        position.startCharacter,
        position.endLine,
        position.endCharacter,
      );
    const toSelectionRange = (position: NodePosition) =>
      new vscode.Range(
        position.startLine,
        position.startCharacter,
        position.startLine,
        position.startCharacter,
      );

    const childNodes = (response?.nodes ?? []).filter((node) => {
      const nodeType = node.metadata?.type;
      return nodeType !== undefined && nodeType in OUTLINE_NODE_TYPES;
    });

    const childSymbols = childNodes.map((node) => {
      const position = positionsByToken.get(node.metadata?.symbolToken ?? 0);
      return new vscode.DocumentSymbol(
        node.displayName || node.metadata?.name || node.description,
        node.description,
        OUTLINE_NODE_TYPES[node.metadata?.type ?? NodeType.Unknown] ??
          vscode.SymbolKind.Object,
        position ? toRange(position) : symbolRange,
        position ? toSelectionRange(position) : symbolRange,
      );
    });

    // If none of the children have a position in the decompiled code (e.g. a
    // namespace, whose code contains no type declarations), the backend reports
    // a position for the node itself - show that instead.
    const selfPosition = positionsByToken.get(nodeMetadata.symbolToken);
    const hasPositionedChild = childNodes.some((node) =>
      positionsByToken.has(node.metadata?.symbolToken ?? 0),
    );
    if (!hasPositionedChild && selfPosition) {
      return [
        new vscode.DocumentSymbol(
          nodeMetadata.name || "<global>",
          nodeMetadata.subType ?? "",
          OUTLINE_NODE_TYPES[nodeMetadata.type] ?? vscode.SymbolKind.Object,
          toRange(selfPosition),
          toSelectionRange(selfPosition),
        ),
      ];
    }

    return childSymbols;
  }
}

export function registerDecompiledDocumentSymbolProvider(
  ilspyBackend: IILSpyBackend,
  getOutputLanguage?: (uri: vscode.Uri) => LanguageName,
) {
  return vscode.languages.registerDocumentSymbolProvider(
    { scheme: ILSPY_URI_SCHEME },
    new DecompiledDocumentSymbolProvider(ilspyBackend, getOutputLanguage),
  );
}
