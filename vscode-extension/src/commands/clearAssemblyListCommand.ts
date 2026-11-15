/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { registerILSpyCommand } from "./commandUtils";

export function registerClearAssemblyListCommand(
  decompiledTreeProvider: DecompiledTreeProvider,
) {
  return registerILSpyCommand("ilspy.clearAssemblyList", async () => {
    await decompiledTreeProvider.clearAssemblies();
  });
}
