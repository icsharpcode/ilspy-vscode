# ILSpy Backend Service

## ILSpyX.Backend

Abstraction layer based on [ICSharpCode.Decompiler](https://www.nuget.org/packages/ICSharpCode.Decompiler/)
and [ICSharpCode.ILSpyX](https://www.nuget.org/packages/ICSharpCode.ILSpyX/) implementing a cross-platform variant of
assembly tree, decompilation, symbol search and analysis features as known from ILSpy desktop application.

## ILSpyX.Backend.LSP

A [Language Server Procotol](https://microsoft.github.io/language-server-protocol/) interface around `ILSpyX.Backend` to
be accessible from the VSCode extension.

Apart from general service initialization, following custom LSP messages are supported:

- `ilspy/initWithAssemblies`
- `ilspy/addAssembly`
- `ilspy/removeAssembly`
- `ilspy/decompileNode`
- `ilspy/getNodes`
- `ilspy/search`
- `ilspy/analyze`

The idea of the interface is a dynamic tree of typed _nodes_ representing the list of assemblies and their contents.
This is similar to the tree presented in ILSpy desktop application.
Clients use `ilspy/getNodes` message to access nodes and their children.
To get the code for a specific node (which may be an assembly, a class, a single method etc.), clients use
`ilspy/decompileNode` message.

## Develop

To build the solution, use command line:

```
git clone https://github.com/icsharpcode/ilspy-vscode.git
cd ilspy-vscode/backend
dotnet build
```

or open `backend/ILSpy-backend.sln` in Visual Studio 2022, Visual Studio Code or JetBrains Rider.

## License

[MIT license](LICENSE.TXT).
