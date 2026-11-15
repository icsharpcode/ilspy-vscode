# ILSpy Visual Studio Code Extension

The ILSpy extension for VS Code is a cross-platform implementation of ILSpy .NET assembly browser and decompiler running inside of Visual Studio Code - in contrast to the [ILSpy desktop application](https://github.com/icsharpcode/ILSpy) running standalone on Windows systems.

![ILSpy VS Code Extension](https://raw.githubusercontent.com/wiki/icsharpcode/ilspy-vscode/images/ilspy_vscode_preview.png)

## Getting Started

* [Installation](https://github.com/icsharpcode/ilspy-vscode/wiki/Installation)
* [Feature Tour](https://github.com/icsharpcode/ilspy-vscode/wiki/Feature-Tour)
    * [Using the Assembly Tree](https://github.com/icsharpcode/ilspy-vscode/wiki/Using-the-Assembly-Tree)
    * [Decompiling Code](https://github.com/icsharpcode/ilspy-vscode/wiki/Decompiling-Code)
    * [Searching for Symbols](https://github.com/icsharpcode/ilspy-vscode/wiki/Searching-for-Symbols)
    * [Analyzing Relationship between Symbols](https://github.com/icsharpcode/ilspy-vscode/wiki/Analyzing-Relationship-between-Symbols)
    * [Using with VS Code Chat](https://github.com/icsharpcode/ilspy-vscode/wiki/Using-with-VS-Code-Chat)
    * [Settings](https://github.com/icsharpcode/ilspy-vscode/wiki/Settings)

## Code Navigation

Decompiled code supports the usual VS Code navigation shortcuts on types, members, local variables and parameters:

| Command | Shortcut (Windows/Linux) | Shortcut (macOS) | Result |
| --- | --- | --- | --- |
| Go to Definition | `F12` or `Ctrl`+Click | `F12` or `Cmd`+Click | Opens the decompiled code of the symbol, also in other assemblies (including .NET framework assemblies) |
| Peek Definition | `Alt`+`F12` | `Option`+`F12` | Shows the definition inline |
| Go to Implementation | `Ctrl`+`F12` | `Cmd`+`F12` | Lists implementations of interface members, overrides of virtual/abstract members and derived types |

Navigation works in C# and IL output. Definitions in the same document are selected in place, otherwise the decompiled code of the symbol's tree node is opened.

## Search Prefixes

Searches cover the loaded assemblies and their dependencies, except assemblies of the .NET framework itself. By default, a search looks for types and members by name. Start the search term with a prefix to search for something else:

| Prefix | Searches for | Example |
| --- | --- | --- |
| *(none)* | Types and members | `HttpClient` |
| `all:` | Everything at once: types, members, literals, namespaces and assemblies | `all:X-Api-Key` |
| `tm:` | Types and members | `tm:HttpClient` |
| `t:` | Types only | `t:HttpClient` |
| `m:` | Members only (methods, fields, properties, events) | `m:SendAsync` |
| `md:` | Methods | `md:SendAsync` |
| `f:` | Fields | `f:_handler` |
| `p:` | Properties | `p:BaseAddress` |
| `e:` | Events | `e:Changed` |
| `c:` | String or numeric literals (constants) | `c:X-Api-Key` |
| `@` | Metadata token | `@06000001` |
| `n:` | Namespaces | `n:System.Net.Http` |
| `a:` | Assemblies by name or file name | `a:System.Net.Http` |
| `an:` | Assemblies by full name | `an:PublicKeyToken=b03f5f7f11d50a3a` |
| `af:` | Assemblies by file path | `af:MyApp.dll` |

You can narrow down any search with `innamespace:` or `inassembly:`, for example `t:Client innamespace:System.Net.Http`. Search terms enclosed in slashes are treated as regular expressions, for example `/^Get.*Async$/`.

Type and member results show the line of the match in their decompiled code (for literals the line using the literal, for names usually the declaration). Opening such a result selects the match. To keep searches fast, lines are shown for the first 100 results only.

Resource search (`r:`) is not supported yet.

See the [Feature Comparison List](https://github.com/icsharpcode/ilspy-vscode/wiki/Feature-Comparison) for the differences between ILSpy desktop application and the VS Code extension.

See our [Wiki](https://github.com/icsharpcode/ilspy-vscode/wiki) for further information.

## Questions, Feedback or Problems?

Get in touch with us on [GitHub](https://github.com/icsharpcode/ilspy-vscode)!
