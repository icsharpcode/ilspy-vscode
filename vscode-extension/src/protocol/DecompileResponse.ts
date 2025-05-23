/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

export default interface DecompileResponse {
  decompiledCode?: string;
  isError: boolean;
  errorMessage?: string;
  shouldUpdateAssemblyList: boolean;
}
