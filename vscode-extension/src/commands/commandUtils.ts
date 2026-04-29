/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import {
  ILSpyCommandArgs,
  ILSpyCommandHandler,
  ILSpyCommandId,
  ILSpyCommandResult,
} from "../extension-api";

type UntypedCommandHandler = (...args: unknown[]) => unknown;

export function registerILSpyCommand<K extends ILSpyCommandId>(
  command: K,
  handler: ILSpyCommandHandler<K>,
  thisArg?: unknown,
): vscode.Disposable {
  return vscode.commands.registerCommand(
    command,
    handler as UntypedCommandHandler,
    thisArg,
  );
}

export function executeILSpyCommand<K extends ILSpyCommandId>(
  command: K,
  ...args: ILSpyCommandArgs<K>
): Thenable<ILSpyCommandResult<K>> {
  return vscode.commands.executeCommand<ILSpyCommandResult<K>>(
    command,
    ...args,
  );
}
