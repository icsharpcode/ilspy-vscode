/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import NodeMetadata from "./NodeMetadata";
import { SymbolModifiers } from "./SymbolModifiers";

export default interface Node {
  metadata?: NodeMetadata;
  displayName: string;
  description: string;
  mayHaveChildren: boolean;
  modifiers: SymbolModifiers;
}
