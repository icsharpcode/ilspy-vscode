/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as os from "os";
import { IDotnetAcquireResult } from "./types";
import OutputWindowLogger from "../OutputWindowLogger";
import {
  cacheDotnetRuntimePath,
  getCachedDotnetRuntimePath,
} from "../decompiler/settings";

const DOTNET_RUNTIME_VERSION = "8.0";

export async function resolveDotnetRuntime(
  context: vscode.ExtensionContext,
  logger: OutputWindowLogger
) {
  let dotnetPath: string | undefined;
  try {
    logger.writeLine(`Checking for .NET runtime v${DOTNET_RUNTIME_VERSION}`);

    const cachedDotnetRuntime = getCachedDotnetRuntimePath(
      context,
      DOTNET_RUNTIME_VERSION
    );

    if (cachedDotnetRuntime) {
      logger.writeLine(
        `Already acquired ${cachedDotnetRuntime}, use that for now`
      );
      dotnetPath = cachedDotnetRuntime;
    }

    acquireDotnetRuntime(context, logger).then((acquiredDotnetRuntime) => {
      cacheDotnetRuntimePath(
        context,
        DOTNET_RUNTIME_VERSION,
        acquiredDotnetRuntime
      );
    });
  } catch (error: any) {
    logger.writeLine(`[ERROR] Acquiring .NET runtime: ${error.toString()}`);
    vscode.window.showWarningMessage(formatAcquireError(error.toString()));
  }

  return dotnetPath;
}

async function acquireDotnetRuntime(
  context: vscode.ExtensionContext,
  logger: OutputWindowLogger
) {
  const requestingExtensionId = context.extension.id;
  let dotnetPath: string | undefined;
  logger.writeLine("Checking for updates...");
  const acquireResult =
    await vscode.commands.executeCommand<IDotnetAcquireResult>(
      "dotnet.acquire",
      { version: DOTNET_RUNTIME_VERSION, requestingExtensionId }
    );
  dotnetPath = acquireResult?.dotnetPath;
  logger.writeLine(
    `Aquiring finished, runtime path is: ${dotnetPath ?? "(not available)"}`
  );

  logger.writeLine("Checking for additional runtime dependencies (Linux only)");
  if (dotnetPath) {
    await vscode.commands.executeCommand("dotnet.ensureDotnetDependencies", {
      command: dotnetPath,
      arguments: ["--info"],
    });
  } else {
    vscode.window.showWarningMessage(formatAcquireError());
  }
  logger.writeLine("Check for additional dependencies finished");

  return dotnetPath;
}

function formatAcquireError(message?: string) {
  return `ILSpy extension won't work without a .NET ${DOTNET_RUNTIME_VERSION} runtime${
    message ? ":" + os.EOL + os.EOL + message : "."
  }`;
}
