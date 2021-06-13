# ILSpy back end service

## ILSpy.Backend

A back end service to provide MSIL decompilations using `ICSharpCode.Decompiler` API, which is based on [Language Service Procotol](https://microsoft.github.io/language-server-protocol/)

A backend service providing a [Language Service Procotol](https://microsoft.github.io/language-server-protocol/) interface to `ICSharpCode.Decompiler` API.

Apart from general service initialization, following custom LSP messages are supported:

- `ilspy/addAssembly`
- `ilspy/removeAssembly`
- `ilspy/decompileAssembly`
- `ilspy/decompileType`
- `ilspy/decompileMember`
- `ilspy/listNamespaces`
- `ilspy/listTypes`
- `ilspy/listMembers`

## Develop

To build the solution, open a Visual Studio 2019 command prompt

```
git clone https://github.com/icsharpcode/ilspy-vscode.git
cd ilspy-vscode
./build compile-backend
```

or open `backend/ILSpy-server.sln` in Visual Studio 2019 (>= 16.9) or another .NET IDE.

## License

[MIT license](LICENSE.TXT).
