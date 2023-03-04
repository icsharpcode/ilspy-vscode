/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompiledCode, LanguageName } from "../protocol/DecompileResponse";
import { getDefaultOutputLanguage } from "./settings";
import IILSpyBackend from "./IILSpyBackend";
import { MemberNode } from "./MemberNode";
import {
  ILSPY_URI_SCHEME,
  ILSPY_URI_SCHEME_LEGACY,
  NodeFromUri,
  uriToMemberNode,
  uriToNode,
} from "./nodeUri";
import { isTypeNode, makeHandle } from "./utils";
import { NodeType } from "../protocol/NodeType";
import { TokenType } from "./TokenType";
import { MemberSubKind } from "./MemberSubKind";

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
    if (uri.scheme === ILSPY_URI_SCHEME_LEGACY) {
      const memberNode = uriToMemberNode(uri);
      if (!memberNode) {
        return "// Invalid URI";
      }
      const code = await this.getCodeFromMemberNode(memberNode);
      return (
        code?.[this.getDocumentOutputLanguage(uri)] ?? "// No code available"
      );
    } else {
      const node = uriToNode(uri);
      if (!node) {
        return "// Invalid URI";
      }
      const code = await this.getCodeFromNode(node);
      return (
        code?.[this.getDocumentOutputLanguage(uri)] ?? "// No code available"
      );
    }
  }

  private async getCodeFromMemberNode(
    element: MemberNode
  ): Promise<DecompiledCode | undefined> {
    if (element.rid === -2) {
      const result = await this.backend.sendDecompileAssembly({
        assemblyPath: element.assembly,
      });
      return result?.decompiledCode;
    }

    if (element.rid === -1) {
      let code: DecompiledCode = {};

      if (element.type === TokenType.NamespaceDefinition) {
        let name = element.name.length === 0 ? "namespace <global>" : "namespace " + element.name;
        code[LanguageName.CSharp] = name + " { }";
        code[LanguageName.IL] = name;
      } else if (element.type === TokenType.AssemblyReference) {
        let s = "";
        if (element.memberSubKind !== MemberSubKind.None) {
          const result = await this.backend.sendListAssemblyReferences({
            assemblyPath: element.assembly
          });
          if (result !== null) {
            for (var ar of result.references.values()) {
              s += `// ${ar}\n`;
            }
          }
        } else {
          s = `// ${element.name}`;
        }
        code[LanguageName.CSharp] = code[LanguageName.IL] = s;
      }

      return Promise.resolve(code);
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

  private async getCodeFromNode(
    element: NodeFromUri
  ): Promise<DecompiledCode | undefined> {
    if (element.type === NodeType.Assembly) {
      const result = await this.backend.sendDecompileAssembly({
        assemblyPath: element.assemblyPath,
      });
      return result?.decompiledCode;
    }

    if (element.type === NodeType.Namespace) {
      let name = element.name.length === 0 ? "<global>" : element.name;
      let namespaceCode: DecompiledCode = {};
      namespaceCode[LanguageName.CSharp] = "namespace " + name + " { }";
      namespaceCode[LanguageName.IL] = "namespace " + name + "";

      return Promise.resolve(namespaceCode);
    }

    if (isTypeNode(element.type)) {
      const result = await this.backend.sendDecompileType({
        assemblyPath: element.assemblyPath,
        handle: element.symbolToken,
      });
      return result?.decompiledCode;
    } else {
      const result = await this.backend.sendDecompileMember({
        assemblyPath: element.assemblyPath,
        type: element.parentSymbolToken,
        member: element.symbolToken,
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

export function registerLegacyDecompilerTextDocumentContentProvider(
  ilspyBackend: IILSpyBackend
) {
  return vscode.workspace.registerTextDocumentContentProvider(
    ILSPY_URI_SCHEME_LEGACY,
    new DecompilerTextDocumentContentProvider(ilspyBackend)
  );
}

export function registerDecompilerTextDocumentContentProvider(
  ilspyBackend: IILSpyBackend
) {
  return vscode.workspace.registerTextDocumentContentProvider(
    ILSPY_URI_SCHEME,
    new DecompilerTextDocumentContentProvider(ilspyBackend)
  );
}
