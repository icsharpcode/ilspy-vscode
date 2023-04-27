/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { MemberNode } from "./decompiler/MemberNode";
import { MemberSubKind } from "./decompiler/MemberSubKind";
import { TokenType } from "./decompiler/TokenType";
import { NodeType } from "./protocol/NodeType";

export const ProductIconMapping = {
  [NodeType.Assembly]: "library",
  [NodeType.Namespace]: "symbol-namespace",
  [NodeType.Event]: "symbol-event",
  [NodeType.Field]: "symbol-field",
  [NodeType.Method]: "symbol-method",
  [NodeType.Enum]: "symbol-enum",
  [NodeType.Class]: "symbol-class",
  [NodeType.Interface]: "symbol-interface",
  [NodeType.Struct]: "symbol-struct",
  [NodeType.Delegate]: "symbol-class",
  [NodeType.Const]: "symbol-constant",
  [NodeType.Property]: "symbol-property",
  [NodeType.ReferencesRoot]: "folder-library",
  [NodeType.AssemblyReference]: "library",
  [NodeType.Unknown]: "question",
};

export function getIconForMemberNode(node: MemberNode): string {
  switch (node.type) {
    case TokenType.AssemblyDefinition:
      return "library";
    case TokenType.NamespaceDefinition:
      return "symbol-namespace";
    case TokenType.EventDefinition:
      return "symbol-event";
    case TokenType.FieldDefinition:
      return "symbol-field";
    case TokenType.MethodDefinition:
      return "symbol-method";
    case TokenType.TypeDefinition:
      switch (node.memberSubKind) {
        case MemberSubKind.Enum:
          return "symbol-enum";
        case MemberSubKind.Interface:
          return "symbol-interface";
        case MemberSubKind.Struct:
          return "symbol-struct";
        default:
          return "symbol-class";
      }
      break;
    case TokenType.LocalConstant:
      return "symbol-constant";
    case TokenType.PropertyDefinition:
      return "symbol-property";
    case TokenType.AssemblyReference:
      return node.memberSubKind === MemberSubKind.Other
        ? "folder-library"
        : "library";
    default:
      return "question";
  }
}
