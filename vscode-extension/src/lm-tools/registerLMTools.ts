/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2025 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import { DecompileNodeTool } from "./DecompileNodeTool";
import { DecompileSymbolTool } from "./DecompileSymbolTool";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { ListAssembliesTool } from "./ListAssembliesTool";
import { AddAssemblyTool } from "./AddAssemblyTool";
import { AnalyzeSymbolTool } from "./AnalyzeSymbolTool";
import { ExportNodeTool } from "./ExportNodeTool";
import { InitializeAssembliesTool } from "./InitializeAssembliesTool";
import { ListNodesTool } from "./ListNodesTool";
import { RemoveAssemblyTool } from "./RemoveAssemblyTool";
import { SearchSymbolsTool } from "./SearchSymbolsTool";

export function registerLMTools(ilspyBackend: IILSpyBackend) {
  return [
    vscode.lm.registerTool(
      InitializeAssembliesTool.Name,
      new InitializeAssembliesTool(ilspyBackend),
    ),
    vscode.lm.registerTool(
      AddAssemblyTool.Name,
      new AddAssemblyTool(ilspyBackend),
    ),
    vscode.lm.registerTool(
      RemoveAssemblyTool.Name,
      new RemoveAssemblyTool(ilspyBackend),
    ),
    vscode.lm.registerTool(
      ListAssembliesTool.Name,
      new ListAssembliesTool(ilspyBackend),
    ),
    vscode.lm.registerTool(
      DecompileSymbolTool.Name,
      new DecompileSymbolTool(ilspyBackend),
    ),
    vscode.lm.registerTool(
      DecompileNodeTool.Name,
      new DecompileNodeTool(ilspyBackend),
    ),
    vscode.lm.registerTool(ListNodesTool.Name, new ListNodesTool(ilspyBackend)),
    vscode.lm.registerTool(
      SearchSymbolsTool.Name,
      new SearchSymbolsTool(ilspyBackend),
    ),
    vscode.lm.registerTool(
      AnalyzeSymbolTool.Name,
      new AnalyzeSymbolTool(ilspyBackend),
    ),
    vscode.lm.registerTool(
      ExportNodeTool.Name,
      new ExportNodeTool(ilspyBackend),
    ),
  ];
}
