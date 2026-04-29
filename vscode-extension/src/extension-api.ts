/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { Node } from "./extension-types";

export interface ILSpyExtensionCommands {
  "ilspy.addAssemblyByPath": (filePath: string) => Promise<void>;
  "ilspy.analyze": (node: Node) => Promise<void>;
  "ilspy.decompileAssemblyInWorkspace": () => Promise<void>;
  "ilspy.decompileAssemblyViaDialog": () => Promise<void>;
  "ilspy.decompileNode": (node: Node, revealInTree?: boolean) => Promise<void>;
  "ilspy.decompileSelectedAssembly": (
    file: vscode.Uri,
  ) => Promise<Node | undefined>;
  "ilspy.export": (node?: Node) => Promise<void>;
  "ilspy.refreshAssemblyList": () => Promise<void>;
  "ilspy.reloadAssembly": (node: Node) => Promise<void>;
  "ilspy.revealNode": (node: Node) => Promise<void>;
  "ilspy.search": (term?: string | Node) => Promise<void>;
  "ilspy.searchEditorSelection": () => Promise<void>;
  "ilspy.selectOutputLanguage": () => Promise<void>;
  "ilspy.unloadAssembly": (node: Node) => Promise<void>;
}

export type ILSpyExtensionCommandId = keyof ILSpyExtensionCommands;
export type ILSpyExtensionCommandArgs<K extends ILSpyExtensionCommandId> =
  Parameters<ILSpyExtensionCommands[K]>;
export type ILSpyExtensionCommandResult<K extends ILSpyExtensionCommandId> =
  Awaited<ReturnType<ILSpyExtensionCommands[K]>>;
export type ILSpyExtensionCommandHandler<K extends ILSpyExtensionCommandId> = (
  ...args: ILSpyExtensionCommandArgs<K>
) => ReturnType<ILSpyExtensionCommands[K]>;

export type ILSpyExtensionApi = {
  executeILSpyCommand: <K extends ILSpyExtensionCommandId>(
    command: K,
    ...args: ILSpyExtensionCommandArgs<K>
  ) => Thenable<ILSpyExtensionCommandResult<K>>;
};
