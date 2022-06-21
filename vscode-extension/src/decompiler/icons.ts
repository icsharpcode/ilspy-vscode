import { MemberNode } from "./MemberNode";
import { MemberSubKind } from "./MemberSubKind";
import { TokenType } from "./TokenType";
import * as path from "path";
import Node from "../protocol/Node";
import { NodeType } from "../protocol/NodeType";

export function getIconImageByTokenType(
  node: MemberNode
): ThenableTreeIconPath {
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

export function getIconImageByNodeType(node: Node): ThenableTreeIconPath {
  let name: string | undefined;

  switch (node.type) {
    case NodeType.Assembly:
      name = "Assembly";
      break;
    case NodeType.Namespace:
      name = "Namespace";
      break;
    case NodeType.Event:
      name = "Event";
      break;
    case NodeType.Field:
      name = "Field";
      break;
    case NodeType.Method:
      name = "Method";
      break;
    case NodeType.Enum:
      name = "EnumItem";
      break;
    case NodeType.Interface:
      name = "Interface";
      break;
    case NodeType.Struct:
      name = "Structure";
      break;
    case NodeType.Const:
      name = "Constant";
      break;
    case NodeType.Property:
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

export function getProductIconByNodeType(
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
    case NodeType.Const:
      return "symbol-constant";
    case NodeType.Property:
      return "symbol-property";
    default:
      return "question";
  }
}

