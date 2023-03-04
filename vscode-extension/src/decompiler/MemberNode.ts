/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { DecompiledCode } from "../protocol/DecompileResponse";
import { MemberSubKind } from "./MemberSubKind";
import { TokenType } from "./TokenType";

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
      this.type === TokenType.NamespaceDefinition ||
      (this.type === TokenType.AssemblyReference && this.memberSubKind !== MemberSubKind.None)
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
