/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as fs from "fs";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";

export function attemptToDecompileFilePath(
  filePath: string,
  decompiledTreeProvider: DecompiledTreeProvider
) {
  let escaped: string = filePath.replace(/\\/g, "\\\\");
  if (escaped[0] === '"' && escaped[escaped.length - 1] === '"') {
    escaped = escaped.slice(1, -1);
  }

  try {
    fs.accessSync(escaped, fs.constants.R_OK);
    decompileFile(escaped, decompiledTreeProvider);
  } catch (err) {
    vscode.window.showErrorMessage("Cannot read the file " + filePath);
  }
}

export async function decompileFile(
  assembly: string,
  decompiledTreeProvider: DecompiledTreeProvider
) {
  await decompiledTreeProvider.addAssembly(assembly);
}
