/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import {
  ILSpyExtensionCommandArgs,
  ILSpyExtensionCommandHandler,
  ILSpyExtensionCommandId,
  ILSpyExtensionCommandResult,
} from "../extension-api";

type UntypedCommandHandler = (...args: unknown[]) => unknown;

export function registerILSpyCommand<K extends ILSpyExtensionCommandId>(
  command: K,
  handler: ILSpyExtensionCommandHandler<K>,
  thisArg?: unknown,
): vscode.Disposable {
  return vscode.commands.registerCommand(
    command,
    handler as UntypedCommandHandler,
    thisArg,
  );
}

export function executeILSpyCommand<K extends ILSpyExtensionCommandId>(
  command: K,
  ...args: ILSpyExtensionCommandArgs<K>
): Thenable<ILSpyExtensionCommandResult<K>> {
  return vscode.commands.executeCommand<ILSpyExtensionCommandResult<K>>(
    command,
    ...args,
  );
}
