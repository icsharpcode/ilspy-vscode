# ILSpy Visual Studio Code Extension [![Visual Studio Marketplace](https://img.shields.io/visual-studio-marketplace/v/icsharpcode.ilspy-vscode)](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode) [![Join the chat at https://gitter.im/icsharpcode/ILSpy](https://badges.gitter.im/icsharpcode/ILSpy.svg)](https://gitter.im/icsharpcode/ILSpy?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![CI](https://github.com/icsharpcode/ilspy-vscode/actions/workflows/ci.yml/badge.svg)](https://github.com/icsharpcode/ilspy-vscode/actions/workflows/ci.yml)

You can install the extension in Visual Studio Code via the [marketplace](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode)

## Getting started

Please see the [Getting started](https://github.com/icsharpcode/ilspy-vscode/wiki/Getting-started) overview.

## What's New

Please see the [Change Log](./vscode-extension/CHANGELOG.md).


## Troubleshooting

If you have issues during installation of .NET Runtime, please see [here](https://github.com/icsharpcode/ilspy-vscode/wiki/Troubleshooting#download-of-net-runtime-fails-with-slow-internet-connection).

## Develop

The extension consists of two parts: The VSCode extension itself (written in TypeScript) and a "backend" server process (written in C#), which provides a bridge to ILSpy functionality.

If first time

```
npm i @vscode/vsce -g
```

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
