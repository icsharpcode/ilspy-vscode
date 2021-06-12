/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import {
  TreeDataProvider,
  EventEmitter,
  TreeItem,
  Event,
  TreeItemCollapsibleState,
  Uri,
  TextDocumentContentProvider,
  CancellationToken,
  ProviderResult,
  window,
} from "vscode";
import { TokenType } from "./TokenType";
import { MemberSubKind } from "./MemberSubKind";
import * as path from "path";
import * as os from "os";
import { DecompiledCode, LanguageName } from "../protocol/DecompileResponse";
import MemberData from "../protocol/MemberData";
import IILSpyBackend from "./IILSpyBackend";
import AssemblyData from "../protocol/AssemblyData";

export class MemberNode {
  private _decompiled?: DecompiledCode;

  constructor(
    private _assembly: string,
    private _name: string,
    private _rid: number,
    private _tokenType: TokenType,
    private _typeDefSubKind: MemberSubKind,
    private _parentToken: number
  ) {}

  public get name(): string {
    return this._name;
  }

  public get rid(): number {
    return this._rid;
  }

  public get type(): TokenType {
    return this._tokenType;
  }

  public get decompiled(): DecompiledCode | undefined {
    return this._decompiled;
  }

  public set decompiled(val: DecompiledCode | undefined) {
    this._decompiled = val;
  }

  public get mayHaveChildren(): boolean {
    return (
      this.type === TokenType.TypeDefinition ||
      this.type === TokenType.AssemblyDefinition ||
      this.type === TokenType.NamespaceDefinition
    );
  }

  public get parent(): number {
    return this._parentToken;
  }

  public get assembly(): string {
    return this._assembly;
  }

  public get memberSubKind(): MemberSubKind {
    return this._typeDefSubKind;
  }
}

interface ThenableTreeIconPath {
  light: string;
  dark: string;
}

export class DecompiledTreeProvider
  implements TreeDataProvider<MemberNode>, TextDocumentContentProvider
{
  private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
  readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;

  constructor(private backend: IILSpyBackend) {}

  public refresh(): void {
    this._onDidChangeTreeData.fire(null);
  }

  public async addAssembly(assembly: string): Promise<boolean> {
    const response = await this.backend.sendAddAssembly({
      assemblyPath: assembly,
    });
    if (response?.added && response?.assemblyData) {
      this.backend.assemblies.set(assembly, response.assemblyData);
      this.refresh();
      return true;
    } else {
      window.showWarningMessage(
        `File '${assembly}' could not be loaded as assembly.`
      );
    }

    return false;
  }

  public async removeAssembly(assembly: string): Promise<boolean> {
    const response = await this.backend.sendRemoveAssembly({
      assemblyPath: assembly,
    });
    if (response?.removed) {
      this.backend.assemblies.delete(assembly);
      this.refresh();
    }
    return response?.removed ?? false;
  }

  public getTreeItem(element: MemberNode): TreeItem {
    return {
      label: element.name,
      tooltip:
        element.type === TokenType.AssemblyDefinition
          ? element.assembly
          : element.name,
      collapsibleState: element.mayHaveChildren
        ? TreeItemCollapsibleState.Collapsed
        : void 0,
      command: {
        command: "showDecompiledCode",
        arguments: [element],
        title: "Decompile",
      },
      contextValue:
        element.type === TokenType.AssemblyDefinition ? "assemblyNode" : void 0,
      iconPath: this.getIconByTokenType(element),
    };
  }

  getIconByTokenType(node: MemberNode): ThenableTreeIconPath {
    let name: string | undefined;

    switch (node.type) {
      case TokenType.AssemblyDefinition:
        name = "Assembly";
        break;
      case TokenType.NamespaceDefinition:
        name = "Namespace";
        break;
      case TokenType.EventDefinition:
        name = "Event";
        break;
      case TokenType.FieldDefinition:
        name = "Field";
        break;
      case TokenType.MethodDefinition:
        name = "Method";
        break;
      case TokenType.TypeDefinition:
        switch (node.memberSubKind) {
          case MemberSubKind.Enum:
            name = "EnumItem";
            break;
          case MemberSubKind.Interface:
            name = "Interface";
            break;
          case MemberSubKind.Struct:
            name = "Structure";
            break;
          default:
            name = "Class";
            break;
        }
        break;
      case TokenType.LocalConstant:
        name = "Constant";
        break;
      case TokenType.PropertyDefinition:
        name = "Property";
        break;
      default:
        name = "Misc";
        break;
    }

    const normalName = name + "_16x.svg";
    const inverseName = name + "_inverse_16x.svg";
    const lightIconPath = path.join(
      __dirname,
      "..",
      "..",
      // "..",
      "resources",
      normalName
    );
    const darkIconPath = path.join(
      __dirname,
      "..",
      "..",
      // "..",
      "resources",
      inverseName
    );

    return {
      light: lightIconPath,
      dark: darkIconPath,
    };
  }

  public getChildren(
    element?: MemberNode
  ): MemberNode[] | Thenable<MemberNode[]> {
    if (this.backend.assemblies.size <= 0) {
      return [];
    }

    // Nothing yet so add assembly nodes
    if (!element) {
      let result = [] as MemberNode[];
      for (let assemblyData of this.backend.assemblies.values()) {
        result.push(
          new MemberNode(
            assemblyData.filePath,
            getAssemblyNodeText(assemblyData),
            -2,
            TokenType.AssemblyDefinition,
            MemberSubKind.None,
            -3
          )
        );
      }

      return result;
    } else if (element.rid === -2) {
      return this.getNamespaces(element.assembly);
    } else if (element.rid === -1) {
      return this.getTypes(element.assembly, element.name);
    } else {
      return this.getMembers(element);
    }
  }

  async getNamespaces(assembly: string): Promise<MemberNode[]> {
    const result = await this.backend.sendListNamespaces({
      assemblyPath: assembly,
    });
    return (
      result?.namespaces.map(
        (n) =>
          new MemberNode(
            assembly,
            n,
            -1,
            TokenType.NamespaceDefinition,
            MemberSubKind.None,
            -2
          )
      ) ?? []
    );
  }

  async getTypes(assembly: string, namespace: string): Promise<MemberNode[]> {
    const result = await this.backend.sendListTypes({
      assemblyPath: assembly,
      namespace: namespace,
    });
    return (
      result?.types.map(
        (t) =>
          new MemberNode(
            assembly,
            t.name,
            getRid(t),
            getHandleKind(t),
            t.subKind,
            -1
          )
      ) ?? []
    );
  }

  async getMembers(element: MemberNode): Promise<MemberNode[]> {
    if (element.mayHaveChildren) {
      const result = await this.backend.sendListMembers({
        assemblyPath: element.assembly,
        handle: makeHandle(element),
      });
      return (
        result?.members.map(
          (m) =>
            new MemberNode(
              element.assembly,
              m.name,
              getRid(m),
              getHandleKind(m),
              m.subKind,
              element.rid
            )
        ) ?? []
      );
    } else {
      return [];
    }
  }

  public async getCode(
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

  public provideTextDocumentContent(
    uri: Uri,
    token: CancellationToken
  ): ProviderResult<string> {
    //TODO:
    return "";
  }
}

function getAssemblyNodeText(assemblyData: AssemblyData) {
  const text = assemblyData.name;
  const additionalData = [assemblyData.version, assemblyData.targetFramework]
    .filter((d) => d)
    .join(", ");
  return `${text}${additionalData ? ` (${additionalData})` : ""}`;
}

// metadata tokens/handles are 32-bit unsigned integers in the format:
// the first byte is the handle kind/token type, the other three bytes are used for the row-id.
function makeHandle(element: MemberNode): number {
  return (element.type << 24) | element.rid;
}

// extract the row-id by removing the first byte
function getRid(member: MemberData): number {
  return member.token & 0x00ffffff;
}

// extract the token/handle kind by shifting the first byte to the position of the first byte
// apply bit-and 0xFF to the result to ensure that the other bytes are zero.
function getHandleKind(member: MemberData): number {
  return (member.token >> 24) & 0xff;
}
