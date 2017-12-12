# ILSpy Visual Studio Code Extension [![Join the chat at https://gitter.im/icsharpcode/ILSpy](https://badges.gitter.im/icsharpcode/ILSpy.svg)](https://gitter.im/icsharpcode/ILSpy?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Build status](https://ci.appveyor.com/api/projects/status/qd6rbpfsparfnwh6/branch/master?svg=true)](https://ci.appveyor.com/project/icsharpcode/ilspy-vscode/branch/master) [![Twitter Follow](https://img.shields.io/twitter/follow/ILSpy.svg?label=Follow%20@ILSpy)](https://twitter.com/ilspy) 

You can install the extension in Visual Studio Code via the [marketplace](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode)
 
 
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
