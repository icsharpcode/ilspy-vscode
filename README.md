## Develop

```
git submodule init
git submodule update
pushd backend\src\ILSpy.Host
dotnet publish -f net461 -c release -r win7-x64 -o  ..\..\..\vscode-extension\bin\ilspy-host
popd
pushd vscode-extension
```

If first time

```
npm i vsce -g
```

If first time, or a new npm package is added,

```
npm i
```

then

```
npm run compile
npm test
vsce package
```

A `.vsix` file should be generated if everything is fine.

To develop and debug the vscode extension, install [Visual Studio Code](https://code.visualstudio.com/),
then run `code .` from the `vscode-extension` folder.
