# ilspy-vscode (Extension)

This is the actual VSCode extension part of the project, which fulfills following tasks:

* Integrates ILSpy UI elements (like Assembly Tree, Search and Analyze panels, palette commands) into VSCode shell
* Manages extension settings (global and workspace-specific)
* Controls [backend](../backend/README.md) process launching it at startup and initializing it with configuration
* Installs a required version of .NET runtime using Microsoft's [.NET Install Tool for Extension Authors](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-runtime)

A description of the extension's features can be found in [Feature Tour](https://github.com/icsharpcode/ilspy-vscode/wiki/Feature-Tour).

## Export decompiled code

To export all decompiled code of a loaded assembly, right-click the assembly node in the `ILSpy: Assemblies` view and run `Export Decompiled Code...`. This will generate a complete C# project including:
- All decompiled source files organized by namespace
- A .csproj project file for easy compilation

## Requirements

- Visual Studio Code >= 1.101
- .NET 10.0 (installed automatically on first start)
- Node.js (22 or newer)
- NPM

## Development

To debug and run the extension, you first need to build and package the backend:

```
cd ..
buildtools/publish-backend
```

The backend's binaries are automatically copied to `vscode-extension/bin/ilspy-backend`, where the extension expects them.

Then initialize the project with

```
npm install
```

Open this directory in Visual Studio Code and start debugging with <kbd>F5</kbd>. A development instance of VS Code will open with the latest extension code running.
