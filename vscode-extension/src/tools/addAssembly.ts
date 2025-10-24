import * as path from "path";
import * as vscode from "vscode";
import * as fs from "fs";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";

export interface AddAssemblyToolInput {
  assemblyPath: string;
}

/**
 * Detects common .NET BCL installation paths
 */
function detectDotNetPaths(): string {
  const platform = process.platform;
  const paths: string[] = [];

  if (platform === "win32") {
    // Check common Windows .NET locations
    const commonPaths = [
      "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App",
      "C:\\Program Files (x86)\\dotnet\\shared\\Microsoft.NETCore.App",
      "C:\\Windows\\Microsoft.NET\\Framework64",
      "C:\\Windows\\Microsoft.NET\\Framework",
    ];

    for (const p of commonPaths) {
      if (fs.existsSync(p)) {
        paths.push(p);
      }
    }
  } else {
    // Check common Unix .NET locations
    const commonPaths = platform === "darwin"
      ? ["/usr/local/share/dotnet/shared/Microsoft.NETCore.App", "/opt/homebrew/share/dotnet/shared/Microsoft.NETCore.App"]
      : ["/usr/share/dotnet/shared/Microsoft.NETCore.App", "/usr/lib/dotnet/shared/Microsoft.NETCore.App"];

    for (const p of commonPaths) {
      if (fs.existsSync(p)) {
        paths.push(p);
      }
    }
  }

  return paths.length > 0
    ? `\n\n.NET BCL locations:\n${paths.map(p => `- \`${p}\``).join('\n')}`
    : "";
}

/**
 * Tool: Add Assembly
 * Loads an assembly file for decompilation
 */
export function registerAddAssemblyTool(
  treeProvider: DecompiledTreeProvider
): vscode.Disposable {
  const dotnetPathsHint = detectDotNetPaths();

  return vscode.lm.registerTool<AddAssemblyToolInput>("ilspy_addAssembly", {
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
          `Failed to load assembly: ${errorMessage}` +
          (dotnetPathsHint ? `\n${dotnetPathsHint}` : "")
        );
      }
    }
  });
}