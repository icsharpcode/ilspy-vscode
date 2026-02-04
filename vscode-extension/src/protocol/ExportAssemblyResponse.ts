/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2025 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

export default interface ExportAssemblyResponse {
  succeeded: boolean;
  outputDirectory?: string;
  filesWritten: number;
  errorCount: number;
  errorMessage?: string;
  shouldUpdateAssemblyList: boolean;
}
