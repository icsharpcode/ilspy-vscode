/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { getDefaultOutputLanguage } from "./settings";
import IILSpyBackend from "./IILSpyBackend";
import { ILSPY_URI_SCHEME, uriToNode } from "./nodeUri";
import NodeMetadata from "../protocol/NodeMetadata";
import { LanguageName } from "../protocol/LanguageName";

export class DecompilerTextDocumentContentProvider
  implements vscode.TextDocumentContentProvider
{
  onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
  onDidChange = this.onDidChangeEmitter.event;

  private documentLanguages: { [url: string]: LanguageName } = {};

  constructor(private backend: IILSpyBackend) {}

  async provideTextDocumentContent(
    uri: vscode.Uri,
    token: vscode.CancellationToken
  ): Promise<string> {
    if (uri.scheme === ILSPY_URI_SCHEME) {
      const nodeMetadata = uriToNode(uri);
      if (!nodeMetadata) {
        return "// Invalid URI";
      }
      const code = await this.getCodeFromNode(
        nodeMetadata,
        this.getDocumentOutputLanguage(uri)
      );
      return code ?? "// No code available";
    }

    return "";
  }

  private async getCodeFromNode(
    nodeMetadata: NodeMetadata,
    outputLanguage: string
  ): Promise<string | undefined> {
    return (
      await this.backend.sendDecompileNode({
        nodeMetadata,
        outputLanguage,
      })
    )?.decompiledCode;
  }

  setDocumentOutputLanguage(uri: vscode.Uri, language: LanguageName) {
    this.documentLanguages[uri.toString()] = language;
    this.onDidChangeEmitter.fire(uri);
  }

  getDocumentOutputLanguage(uri: vscode.Uri | undefined): LanguageName {
    if (uri === undefined) {
      return getDefaultOutputLanguage();
    }
    return this.documentLanguages[uri.toString()] ?? getDefaultOutputLanguage();
  }
}

export function registerDecompilerTextDocumentContentProvider(
  ilspyBackend: IILSpyBackend
) {
  return vscode.workspace.registerTextDocumentContentProvider(
    ILSPY_URI_SCHEME,
    new DecompilerTextDocumentContentProvider(ilspyBackend)
  );
}
