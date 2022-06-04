/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import Node from "./Node";

export default interface NodeData {
  node?: Node;
  name: string;
  description: string;
  mayHaveChildren: boolean;
}
