# ilspy-vscode (powered by ICSharpCode.Decompiler)

Welcome to the ILSpy extension for Visual Studio Code! This extension provides two commands inside Visual Studio Code:

- `ilspy.decompileAssemblyInWorkspace` - Decompile an MSIL assembly inside the current Visual Studio Code workspace.
- `ilspy.decompileAssemblyViaDialog` - Decompile an MSIL assembly from file picker dialog.

Open the Visual Studio Code Command Palette (<kbd>Ctrl+Shift+P</kbd>) then type `ilspy` to show the two commands.

The `Decompile IL Assembly in Current Workspace` will put all potential .NET assemblies
(files with extension `.dll`, `.exe`, `.winmd`, `.netmodule`, `.wasm` or `.nupkg`) in your
workspace in a list for selection.

The `Decompile IL Assembly (pick file)` command allows choosing an assembly using the file picker dialog. The dialog
should also allow one to type in the full path, for example, `c:/temp/a.dll` or `/home/user/b.dll`.

If the file is a valid .NET assembly or NuGet package, a tree view named `ILSPY: ASSEMBLIES` is added into the Explorer view.
It allows expanding and selecting various nodes, whose decompiled C# code is shown in the editor.

Loaded assemblies can be closed by hovering the assemly nodes and clicking the "X" icon.

Further features are:

- Symbol search across all loaded assemblies
- Decompiling symbols to IL and different C# versions
- Analyzing references between symbols across loaded assemblies
- Saving list of loaded assemblies along with currently open workspace

## Requirements

- Visual Studio Code >= 1.101
- .NET 10.0 (installed automatically on first start)

## What's New

See our [change log](https://github.com/icsharpcode/ilspy-vscode/blob/master/vscode-extension/CHANGELOG.md) for all of the updates.

### Found a Bug?

Please file any issues at https://github.com/icsharpcode/ilspy-vscode/issues

## Development

First install:

- Node.js (22 or newer)
- NPM

- Compile and prepare ILSpy.Backend:

```
cd ..
buildtools/build-backend
```

- Run `npm i`
- Run `npm run compile`
- Open in Visual Studio Code (`code .`)
- _Optional:_ run `npm run watch`, make code changes
- Press <kbd>F5</kbd> to debug

To **test** do the following: `npm test` or <kbd>F5</kbd> in VS Code with the "Launch Tests" debug configuration.

## License

[MIT license](https://github.com/icsharpcode/ilspy-vscode/blob/master/vscode-extension/LICENSE.TXT).
