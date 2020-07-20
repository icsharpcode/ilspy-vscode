# ilspy-vscode (powered by ICSharpCode.Decompiler)

Welcome to the ILSpy extension for Visual Studio Code! This extension provides two commands inside Visual Studio Code:

* `ilspy.decompileAssemblyInWorkspace` - Decompile an MSIL assembly inside the current Visual Studio Code workspace.
* `ilspy.decompileAssemblyViaDialog` - Decompile an MSIL assembly from file picker dialog.

Open the Visual Studio Code Command Palette (<kbd>Ctrl+Shift+P</kbd>) then type `ilspy` to show the two commands.

The `Decompile IL Assembly in Current Workspace` will put all potential .NET assemblies
(files with extension `.dll`, `.exe`, `.winrt`, or `.netmodule`) in your
workspace in a list for selection.

The `Decompile IL Assembly (pick file)` command allows choosing an assembly using the file picker dialog. The dialog
should also allow one to type in the full path, for example, `c:/temp/a.dll` or `/home/user/b.dll`.

If the file is a valid .NET assembly, a tree view named `ILSPY DECOMPILED MEMBERS` is added into the Explorer view.
It allows expanding and selecting various nodes, whose decompiled C# code is shown in the editor.

Loaded assemblies can be closed by right-click on the assemly nodes to show the context menu then select `Unload Assembly` menu item.

## Requirements

* Windows - no other pre-requisites
* Linux/MacOS - requires Mono version >= 4.6.0.

## What's New

First release of ILSpy .NET Decompiler extension for Visual Studio Code!

See our [change log](https://github.com/icsharpcode/ilspy-vscode/blob/master/vscode-extension/CHANGELOG.md) for all of the updates.

### Found a Bug?
Please file any issues at https://github.com/icsharpcode/ilspy-vscode/issues

## Development

First install:
* Node.js (newer than 4.3.1)
* Npm (newer than 2.14.12)

* Run `npm i`
* Run `npm run compile`
* Open in Visual Studio Code (`code .`)
* *Optional:* run `npm run watch`, make code changes
* Press <kbd>F5</kbd> to debug

To **test** do the following: `npm test` or <kbd>F5</kbd> in VS Code with the "Launch Tests" debug configuration.

## License

[MIT license](https://github.com/icsharpcode/ilspy-vscode/blob/master/vscode-extension/LICENSE.TXT).
