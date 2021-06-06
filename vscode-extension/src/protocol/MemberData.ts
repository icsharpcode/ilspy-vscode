/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { MemberSubKind } from "../decompiler/MemberSubKind";

export default interface MemberData {
  name: string;
  token: number;
  subKind: MemberSubKind;
}
