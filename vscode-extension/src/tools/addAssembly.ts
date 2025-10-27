import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";

export interface LoadAssemblyToolInput {
  assemblyPath: string;
}

/**
 * Tool: Load Assembly
 * Loads an assembly file for decompilation
 */
export function registerLoadAssemblyTool(
  treeProvider: DecompiledTreeProvider
): vscode.Disposable {
  return vscode.lm.registerTool<LoadAssemblyToolInput>("ilspy_load_assembly", {
    async prepareInvocation(
      options,
      _token
    ) {
      const { assemblyPath } = options.input;
      return {
        invocationMessage: new vscode.MarkdownString(`$(library) Loading assembly: \`${assemblyPath}\``),
      };
    },

    async invoke(
      options,
      _token
    ) {
      const { assemblyPath } = options.input;

      try {
        const success = await treeProvider.addAssembly(assemblyPath);

        if (!success) {
          throw new Error(`Failed to load assembly: ${assemblyPath}`);
        }

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(`Successfully loaded: ${assemblyPath}`),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(
          `Failed to load assembly: ${errorMessage}. Use ilspy_list_runtime_locations to discover .NET BCL assemblies.`
        );
      }
    }
  });
}