# msil-decompiler (powered by ILSpy)

Welcome to the msil-decompiler extension for Visual Studio Code! This prototype provides two commands inside Visual Studio Code:

* `msildecompiler.decompileAssemblyPromptForFilePath` - Decompile an MSIL assembly from a user-input full path.
* `msildecompiler.decompileAssemblyInWorkspace` - Decompile a MSIL assembly inside of current Visual Studio Code workspace.

## Requirements

* Windows - no other pre-requisites
* Linux/MacOS - requires Mono version >= 4.6.0.

## Release Notes

### 0.0.2

Enable *Nix support using Mono.

### 0.0.1

Initial release

## Development

First install:
* Node.js (newer than 4.3.1)
* Npm (newer than 2.14.12)

* Run `npm i`
* Run `npm run compile`
* Open in Visual Studio Code (`code .`)
* *Optional:* run `npm run watch`, make code changes
* Press <kbd>F5</kbd> to debug

To **test** do the following: `npm run test` or <kbd>F5</kbd> in VS Code with the "Launch Tests" debug configuration.

## License

TBD