/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { ILSpyCommandHandler, ILSpyCommandId } from "../extension-api";

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
