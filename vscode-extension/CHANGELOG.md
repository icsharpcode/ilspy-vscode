# Change Log

All notable changes to the "ilspy-vscode" extension will be documented in this file.

## 0.17

- Upgrade to ICSharpCode.Decompiler 8.2 (see release notes for [ILSpy 8.2](https://github.com/icsharpcode/ILSpy/releases/tag/v8.2) to get more details)
- Backend process now migrated to .NET 8, so extension locally downloads a .NET 8 runtime on startup
- Search button not shown in "ILSPY: Assemblies" view anymore, when no assemblies were loaded

## 0.16.1

- Fixes for 2 issues with adding assemblies from workspace

## 0.16

- List of decompiled assemblies is now persisted per workspace: Assemblies will appear again as soon as workspace is reloaded. This behavior can be disabled in VS Code settings.
- Decompile any assembly directly from Explorer view
- Small improvements in assembly list (buttons instead of context menus)

## 0.15

- Assembly references are now shown in tree
- Fix for failing unload/reload of assemblies in tree

## 0.14.1

- Fix for missing output language selection

## 0.14

- Introduction of symbol search
- New "Reload assembly" command in "Decompiled Members" tree
- "Decompiled Members" now properly sorted
- Usage of VS Code built-in icons for different symbol types instead of shipping our own icon set
- Extension package (VSIX) now optimized and smaller in size
- Extension is now loaded a bit later to avoid slowing down VS Code startup unnecessarily

## 0.13.1

- Fix zombie backend processes still running after VS Code was closed on Linux and macOS
- Make output language selection less confusing

## 0.13

- Upgrade to .NET 6.0 runtime
- "ILSpy Decompiled Members" view now has a menu to quickly add assemblies without command palette

## 0.12.1

- Fix issue with "Pick assembly from file system" command, where only namespaces were decompiled.

## 0.12

- Only one document is opened now on decompilation (C# or IL). Users can switch the output language in editor toolbar and also preset a default output language in VS Code configuration. Documents are read-only now.
- Previously opened documents with decompiled code are no longer re-opened on restart of VS Code
- Improved decompilation results by resolving dependencies of decompiled assembly (as ILSpy does)

## 0.11.2

- Fixed issues activating the extension on Linux systems, also leading to malfunction of some other extensions

## 0.11

- Re-architecture of internal ILSpy interface (_ILSpy.Backend_) based on .NET 5.0, therefore no Mono dependency on Linux/MacOS any more
- Improved look of assembly tree showing assembly names and metadata instead of a too long path
- Update to v7.1 of `ICSharpCode.Decompiler`

## 0.10

- Update to v7.0 of `ICSharpCode.Decompiler`

## 0.9

- Update to v6.1 of `ICSharpCode.Decompiler`

## 0.8

- Update to v6.0 of `ICSharpCode.Decompiler`

## 0.7.11

- Use temp directory and files to show decompiled documents.
- Update dev dependencies to address npm audit warnings

## 0.7.10

- Update to ILSpy 5.0.2

## 0.7.9

- Update npm pacakges
- Update to ILSpy 5.0.1

## 0.7.8

- Update to v5.0 of `ICSharpCode.Decompiler`

## 0.7.7

- Reuse same doc window to show decompiled code

## 0.7.6

- Update backend to target `net472`

## 0.7.5

- Update to v4.0 of `ICSharpCode.Decompiler`

## 0.7.0

- Add IL view
- Add a context menu for assembly nodes to unload them.

## 0.6.0

- Allows choosing assemblies using the file picker dialog
- Upgrade to v4 of `ICSharpCode.Decompiler`

## 0.5.0

Update to v3.2 of `ICSharpCode.Decompiler`

## 0.4.0

Update to v3.1 of `ICSharpCode.Decompiler`

## 0.3.0

Update to RTM version of `ICSharpCode.Decompiler`

## 0.2.0

- Publish to the marketplace (fix icon and more)
- Update to latest ICS.Decompiler NuGet (Beta 4)

## 0.1.0

Add more details about the two commands in README

## 0.0.3

Move to icsharpcode/ilspy-vscode repository

## 0.0.2

Enable \*Nix support using Mono.

## 0.0.1

Initial release
