/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

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

