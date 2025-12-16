# ILSpy Visual Studio Code Extension [![Visual Studio Marketplace](https://img.shields.io/visual-studio-marketplace/v/icsharpcode.ilspy-vscode?label=VS%20Marketplace)](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode)  [![Open VSX](https://img.shields.io/open-vsx/v/icsharpcode/ilspy-vscode?label=Open%20VSX&color=blue)](https://open-vsx.org/extension/icsharpcode/ilspy-vscode) [![Join the chat at https://gitter.im/icsharpcode/ILSpy](https://badges.gitter.im/icsharpcode/ILSpy.svg)](https://gitter.im/icsharpcode/ILSpy?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![CI](https://github.com/icsharpcode/ilspy-vscode/actions/workflows/ci.yml/badge.svg)](https://github.com/icsharpcode/ilspy-vscode/actions/workflows/ci.yml)

The ILSpy extension for VS Code is an implementation of ILSpy .NET assembly browser and decompiler running inside of Visual Studio Code UI - in contrast to the [ILSpy desktop application](https://github.com/icsharpcode/ILSpy) running standalone on Windows systems.

Please see the [Feature Comparison List](https://github.com/icsharpcode/ilspy-vscode/wiki/Feature-Comparison) for the differences between ILSpy desktop application and the VS Code extension.

## Getting started

You can [install](https://github.com/icsharpcode/ilspy-vscode/wiki/Installation) the extension in Visual Studio Code via the [VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode) or [Open VSX](https://open-vsx.org/extension/icsharpcode/ilspy-vscode).

Please see the [Feature Tour](https://github.com/icsharpcode/ilspy-vscode/wiki/Feature-Tour) to explore the extension's features.

## What's New

Please see the [Change Log](./vscode-extension/CHANGELOG.md).

## Troubleshooting

If you have issues during installation of .NET Runtime, please see [here](https://github.com/icsharpcode/ilspy-vscode/wiki/Troubleshooting#download-of-net-runtime-fails-with-slow-internet-connection).

## Develop

The extension consists of two parts: The VS Code extension itself (written in TypeScript) and a "backend" server process (written in C#), which provides a bridge to ILSpy functionality.

Compile and package all parts:

```
./buildtools/publish-backend
./buildtools/build-vsix
```

An installable `.vsix` file should be generated in `artifacts` folder, if everything is fine.

Compile only backend server from console:

```
cd backend
dotnet build
```

Run backend tests:

```
cd backend
dotnet test
```

Or open `backend/ILSpy-backend.sln` in Visual Studio 2022, Visual Studio Code or JetBrains Rider.

To develop and debug the VSCode extension, install [Visual Studio Code](https://code.visualstudio.com/),
then run

```
cd vscode-extension
code .
```

The backend code must have been built at least once (as seen above) for the extension to run.
