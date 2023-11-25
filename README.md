# ILSpy Visual Studio Code Extension [![Join the chat at https://gitter.im/icsharpcode/ILSpy](https://badges.gitter.im/icsharpcode/ILSpy.svg)](https://gitter.im/icsharpcode/ILSpy?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![CI](https://github.com/icsharpcode/ilspy-vscode/actions/workflows/ci.yml/badge.svg)](https://github.com/icsharpcode/ilspy-vscode/actions/workflows/ci.yml) [![Twitter Follow](https://img.shields.io/twitter/follow/ILSpy.svg?label=Follow%20@ILSpy)](https://twitter.com/ilspy)

You can install the extension in Visual Studio Code via the [marketplace](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode)

## Getting started

Please see the [Getting started](https://github.com/icsharpcode/ilspy-vscode/wiki/Getting-started) overview.

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

Or open `backend/ILSpy-server.sln` in Visual Studio 2022 (>= 17.8) or another .NET IDE.

To develop and debug the VSCode extension, install [Visual Studio Code](https://code.visualstudio.com/),
then run

```
cd vscode-extension
code .
```
