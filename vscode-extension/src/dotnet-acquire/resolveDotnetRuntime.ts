/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as os from "os";
import { existsSync } from "fs";
import { IDotnetAcquireResult } from "./types";
import OutputWindowLogger from "../OutputWindowLogger";
import {
  cacheDotnetRuntimePath,
  getCachedDotnetRuntimePath,
} from "../decompiler/settings";

const DOTNET_RUNTIME_VERSION = "10.0";

export interface ResolvedDotnetRuntime {
  dotnetPath?: string;
  refreshRuntimeInBackground?: () => Promise<void>;
}

export async function resolveDotnetRuntime(
  context: vscode.ExtensionContext,
  logger: OutputWindowLogger,
): Promise<ResolvedDotnetRuntime> {
  let dotnetPath: string | undefined;
  let refreshRuntimeInBackground: (() => Promise<void>) | undefined;
  try {
    logger.writeLine(`Checking for .NET runtime v${DOTNET_RUNTIME_VERSION}`);

    const cachedDotnetRuntime = getCachedDotnetRuntimePath(
      context,
      DOTNET_RUNTIME_VERSION,
    );

    if (cachedDotnetRuntime && existsSync(cachedDotnetRuntime)) {
      logger.writeLine(
        `Already acquired ${cachedDotnetRuntime}, use that for now and update after backend startup`,
      );
      dotnetPath = cachedDotnetRuntime;
      refreshRuntimeInBackground = async () => {
        try {
          const acquiredDotnetRuntime = await acquireDotnetRuntime(
            context,
            logger,
          );
          if (acquiredDotnetRuntime) {
            cacheDotnetRuntimePath(
              context,
              DOTNET_RUNTIME_VERSION,
              acquiredDotnetRuntime,
            );
          }
        } catch (error: any) {
          logger.writeLine(
            `[WARNING] Background .NET runtime update failed: ${error.toString()}`,
          );
        }
      };
    } else {
      if (cachedDotnetRuntime) {
        logger.writeLine(
          `Cached runtime ${cachedDotnetRuntime} is no longer available, acquiring a fresh runtime`,
        );
      } else {
        logger.writeLine(
          `No known installed runtime for v${DOTNET_RUNTIME_VERSION}, wait until it's acquired...`,
        );
      }
      const acquiredDotnetRuntime = await acquireDotnetRuntime(context, logger);
      dotnetPath = acquiredDotnetRuntime;
      if (acquiredDotnetRuntime) {
        cacheDotnetRuntimePath(
          context,
          DOTNET_RUNTIME_VERSION,
          acquiredDotnetRuntime,
        );
      }
    }
  } catch (error: any) {
    logger.writeLine(`[ERROR] Acquiring .NET runtime: ${error.toString()}`);
    vscode.window.showWarningMessage(formatAcquireError(error.toString()));
  }

  return {
    dotnetPath,
    refreshRuntimeInBackground,
  };
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
