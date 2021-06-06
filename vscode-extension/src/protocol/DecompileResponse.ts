/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

export enum LanguageName {
  CSharp = "cs",
  IL = "il",
}

export interface DecompiledCode {
  [index: string]: string;
}

export default interface DecompileResponse {
  decompiledCode: DecompiledCode;
  isError: boolean;
  errorMessage?: string;
}
