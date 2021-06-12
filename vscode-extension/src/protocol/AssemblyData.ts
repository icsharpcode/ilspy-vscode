/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

export default interface AssemblyData {
  name: string;
  filePath: string;
  version?: string;
  targetFramework?: string;
}
