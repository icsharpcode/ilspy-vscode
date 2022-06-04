/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { NodeType } from "./NodeType";

export default interface Node {
  type: NodeType;
  symbolToken: number;
  parentSymbolToken: number;
}
