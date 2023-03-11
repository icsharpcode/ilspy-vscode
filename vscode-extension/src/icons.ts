/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { MemberNode } from "./decompiler/MemberNode";
import { MemberSubKind } from "./decompiler/MemberSubKind";
import { TokenType } from "./decompiler/TokenType";
import Node from "./protocol/Node";
import { NodeType } from "./protocol/NodeType";

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
      return node.memberSubKind === MemberSubKind.Other ? "folder-library" : "library";
    default:
      return "question";
  }
}

export function getProductIconForNodeType(
  nodeType: NodeType | undefined
): string {
  switch (nodeType) {
    case NodeType.Assembly:
      return "library";
    case NodeType.Namespace:
      return "symbol-namespace";
    case NodeType.Event:
      return "symbol-event";
    case NodeType.Field:
      return "symbol-field";
    case NodeType.Method:
      return "symbol-method";
    case NodeType.Enum:
      return "symbol-enum";
    case NodeType.Class:
      return "symbol-class";
    case NodeType.Interface:
      return "symbol-interface";
    case NodeType.Struct:
      return "symbol-struct";
    case NodeType.Delegate:
      return "symbol-class";
    case NodeType.Const:
      return "symbol-constant";
    case NodeType.Property:
      return "symbol-property";
    default:
      return "question";
  }
}
