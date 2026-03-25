# ILSpy VS Code ↔ Backend LSP Message Protocol

This document describes the custom LSP (JSON-RPC) requests used between the VS Code extension (`vscode-extension`) and the backend language server (`ILSpyX.Backend.LSP`).

## Transport and conventions

- Transport: Language Server Protocol over stdio.
- Message kind: custom **requests** (client → server), no custom notifications are currently defined.
- Method namespace: all custom methods are prefixed with `ilspy/`.
- Parameter encoding: named-object parameters (`ParameterStructures.byName`), not positional arrays.

## Custom requests

| Method | Request params | Response | Description |
| --- | --- | --- | --- |
| `ilspy/initWithAssemblies` | `assemblyPaths: string[]` | `loadedAssemblies?: AssemblyData[]` | Preloads a list of assemblies for assembly list (e.g. as loaded from configuration) |
| `ilspy/addAssembly` | `assemblyPath: string` | `added: boolean`, `assemblyData?: AssemblyData` | Adds a single assembly or NuGet package to assembly list. |
| `ilspy/removeAssembly` | `assemblyPath: string` | `removed: boolean` | Removes a single assembly from assembly list. |
| `ilspy/getNodes` | `nodeMetadata?: NodeMetadata` | `nodes?: Node[]`, `shouldUpdateAssemblyList: boolean` | Retrieves a list of child nodes of a node - or root nodes, if `nodeMetadata === null` |
| `ilspy/decompileNode` | `nodeMetadata: NodeMetadata`, `outputLanguage: string` | `decompiledCode?: string`, `isError: boolean`, `errorMessage?: string`, `shouldUpdateAssemblyList: boolean` | Retrieves the (decompiled) code behind a node. |
| `ilspy/exportNode` | `nodeMetadata: NodeMetadata`, `outputLanguage: string`, `outputDirectory: string`, `includeCompilerGenerated: boolean` | `succeeded: boolean`, `outputDirectory?: string`, `filesWritten: number`, `errorCount: number`, `errorMessage?: string`, `shouldUpdateAssemblyList: boolean` | Exports node contents to the file system. |
| `ilspy/search` | `term: string` | `results: Node[]`, `shouldUpdateAssemblyList: boolean` | Searches for nodes in loaded assemblies using a free-text term. |
| `ilspy/analyze` | `nodeMetadata?: NodeMetadata` | `results: Node[]`, `shouldUpdateAssemblyList: boolean` | Analyzes a node and gets the list of related nodes (like types implementing a specific interface, callers of a method etc.) |

## Shared payload types

### `AssemblyData`

- `name: string`
- `filePath: string`
- `isAutoLoaded: boolean`
- `version?: string`
- `targetFramework?: string`

### `NodeMetadata`

- `assemblyPath: string` \
  Path and file name of assembly or NuGet package containing the node
- `bundledAssemblyName?: string` \
  If the node originates from an assembly bundled in a NuGet package file, this field contains the name of the assembly file.
- `type: NodeType` \
  Type of node metadata. Determines the node icon in frontends.
- `name: string` \
  Node/symbol name.
- `symbolToken: number` \
  Token handle of the symbol represented by the node. Usually set and used by backend.
- `parentSymbolToken: number` \
  Token handle of the parent symbol (usually references the type symbol for type member nodes)
- `subType?: string` \
  Additional (domain-specific) free-text type classifying the node.
- `availableCommands: AvailableNodeCommands` \
  Bit-flag set describing which commands are available for the node.

### `AvailableNodeCommands`

- `None = 0`
- `Decompile = 1`
- `Analyze = 2`
- `Export = 4`
- `ManageRootEntries = 8`

### `Node`

- `metadata?: NodeMetadata`
- `displayName: string`
- `description: string`
- `mayHaveChildren: boolean`
- `modifiers: SymbolModifiers`
- `flags: NodeFlags`

### `DecompileResponse`

- `decompiledCode?: string`
- `isError: boolean`
- `errorMessage?: string`
- `shouldUpdateAssemblyList: boolean`

### `ExportNodeResponse`

- `succeeded: boolean`
- `outputDirectory?: string`
- `filesWritten: number`
- `errorCount: number`
- `errorMessage?: string`
- `shouldUpdateAssemblyList: boolean`

## Behavior notes

- `shouldUpdateAssemblyList` becomes `true` when executing the request causes a change in the count of auto-loaded assemblies (for example, dependency auto-load while expanding/decompiling/searching/analyzing).
- `outputLanguage` for `ilspy/decompileNode` supports values used by backend language settings (for example `il`, `cs-1` … `cs-12`).
