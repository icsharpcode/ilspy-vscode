/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as os from "os";
import { IDotnetAcquireResult } from "./types";
import ILSpyBackend from "../decompiler/ILSpyBackend";

const netRuntimeVersion = "5.0";

export async function acquireDotnetRuntime(context: vscode.ExtensionContext) {
  const requestingExtensionId = context.extension.id;

  let dotnetPath: string | undefined;
  try {
    const acquireResult =
      await vscode.commands.executeCommand<IDotnetAcquireResult>(
        "dotnet.acquire",
        { version: netRuntimeVersion, requestingExtensionId }
      );
    dotnetPath = acquireResult?.dotnetPath;
  } catch (error) {
    vscode.window.showWarningMessage(formatAcquireError(error.toString()));
  }

  if (!dotnetPath) {
    vscode.window.showWarningMessage(formatAcquireError());
  }

  await vscode.commands.executeCommand("dotnet.ensureDotnetDependencies", {
    command: dotnetPath,
    arguments: [ILSpyBackend.getExecutable(context)],
  });

  return dotnetPath;
}

function formatAcquireError(message?: string) {
  return `ILSpy extension won't work without a .NET ${netRuntimeVersion} runtime${
    message ? ":" + os.EOL + os.EOL + message : "."
  }`;
}
