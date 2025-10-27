import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";

export interface ListRuntimeLocationsToolInput {}

export interface RuntimeLocation {
  path: string;
  version: string;
  name: string;
}

/**
 * Detects .NET runtime installations on the system
 * Returns available .NET runtime locations with paths and versions
 */
export function detectDotNetRuntimeLocations(): RuntimeLocation[] {
  const platform = process.platform;
  const locations: RuntimeLocation[] = [];

  if (platform === "win32") {
    // Windows .NET locations
    const basePaths = [
      "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App",
      "C:\\Program Files (x86)\\dotnet\\shared\\Microsoft.NETCore.App",
      "C:\\Windows\\Microsoft.NET\\Framework64",
      "C:\\Windows\\Microsoft.NET\\Framework",
    ];

    for (const basePath of basePaths) {
      if (fs.existsSync(basePath)) {
        try {
          const versions = fs.readdirSync(basePath, { withFileTypes: true })
            .filter(dirent => dirent.isDirectory())
            .map(dirent => dirent.name);

          for (const version of versions) {
            const fullPath = path.join(basePath, version);
            locations.push({
              path: fullPath,
              version: version,
              name: basePath.includes("Framework") ? ".NET Framework" : ".NET Core/5+"
            });
          }
        } catch (error) {
          // Skip if can't read directory
        }
      }
    }
  } else {
    // Unix-like systems (macOS, Linux)
    const basePaths = platform === "darwin"
      ? [
          "/usr/local/share/dotnet/shared/Microsoft.NETCore.App",
          "/opt/homebrew/share/dotnet/shared/Microsoft.NETCore.App"
        ]
      : [
          "/usr/share/dotnet/shared/Microsoft.NETCore.App",
          "/usr/lib/dotnet/shared/Microsoft.NETCore.App"
        ];

    for (const basePath of basePaths) {
      if (fs.existsSync(basePath)) {
        try {
          const versions = fs.readdirSync(basePath, { withFileTypes: true })
            .filter(dirent => dirent.isDirectory())
            .map(dirent => dirent.name);

          for (const version of versions) {
            const fullPath = path.join(basePath, version);
            locations.push({
              path: fullPath,
              version: version,
              name: ".NET Core/5+"
            });
          }
        } catch (error) {
          // Skip if can't read directory
        }
      }
    }
  }

  return locations;
}

/**
 * Tool: List Runtime Locations
 * Returns available .NET runtime installations with paths and versions
 */
export function registerListRuntimeLocationsTool(): vscode.Disposable {
  return vscode.lm.registerTool<ListRuntimeLocationsToolInput>("ilspy_list_runtime_locations", {
    async prepareInvocation(
      _options,
      _token
    ) {
      return {
        invocationMessage: new vscode.MarkdownString("$(server-environment) Discovering .NET runtime locations..."),
      };
    },

    async invoke(
      _options,
      _token
    ) {
      try {
        const locations = detectDotNetRuntimeLocations();

        if (locations.length === 0) {
          return new vscode.LanguageModelToolResult([
            new vscode.LanguageModelTextPart(
              "No .NET runtime installations found on this system."
            ),
          ]);
        }

        // Group by base path for better organization
        const grouped = locations.reduce((acc, loc) => {
          const key = loc.name;
          if (!acc[key]) {
            acc[key] = [];
          }
          acc[key].push(loc);
          return acc;
        }, {} as Record<string, RuntimeLocation[]>);

        return new vscode.LanguageModelToolResult([
          new vscode.LanguageModelTextPart(
            JSON.stringify(
              {
                totalLocations: locations.length,
                runtimesByType: grouped,
                allLocations: locations
              },
              null,
              2
            )
          ),
        ]);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        throw new Error(`Failed to detect .NET runtimes: ${errorMessage}`);
      }
    }
  });
}
