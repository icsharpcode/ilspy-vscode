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

## Search Prefixes

By default, a search looks for types and members by name. Start the search term with a prefix to search for something else:

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

Resource search (`r:`) is not supported yet.

See the [Feature Comparison List](https://github.com/icsharpcode/ilspy-vscode/wiki/Feature-Comparison) for the differences between ILSpy desktop application and the VS Code extension.

See our [Wiki](https://github.com/icsharpcode/ilspy-vscode/wiki) for further information.

## Questions, Feedback or Problems?

Get in touch with us on [GitHub](https://github.com/icsharpcode/ilspy-vscode)!
