/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledCode, LanguageName } from "../protocol/DecompileResponse";
import { getDefaultOutputLanguage } from "./settings";
import IILSpyBackend from "./IILSpyBackend";
import { MemberNode } from "./MemberNode";
import { ILSPY_URI_SCHEME, uriToMemberNode } from "./memberNodeUri";
import { makeHandle } from "./utils";

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
    const memberNode = uriToMemberNode(uri);
    if (!memberNode) {
      return "// Invalid URI";
    }
    const code = await this.getCode(memberNode);
    return (
      code?.[this.getDocumentOutputLanguage(uri)] ?? "// No code available"
    );
  }

  private async getCode(
    element: MemberNode
  ): Promise<DecompiledCode | undefined> {
    if (element.rid === -2) {
      const result = await this.backend.sendDecompileAssembly({
        assemblyPath: element.assembly,
      });
      return result?.decompiledCode;
    }

    if (element.rid === -1) {
      let name = element.name.length === 0 ? "<global>" : element.name;
      let namespaceCode: DecompiledCode = {};
      namespaceCode[LanguageName.CSharp] = "namespace " + name + " { }";
      namespaceCode[LanguageName.IL] = "namespace " + name + "";

      return Promise.resolve(namespaceCode);
    }

    if (element.mayHaveChildren) {
      const result = await this.backend.sendDecompileType({
        assemblyPath: element.assembly,
        handle: makeHandle(element),
      });
      return result?.decompiledCode;
    } else {
      const result = await this.backend.sendDecompileMember({
        assemblyPath: element?.assembly,
        type: element?.parent,
        member: makeHandle(element),
      });
      return result?.decompiledCode;
    }
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
