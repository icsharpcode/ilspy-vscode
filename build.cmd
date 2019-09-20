set build_version=local

if NOT '%1'=='' set build_version=%1

git submodule init
git submodule update

pushd backend
dotnet restore
dotnet msbuild
popd

pushd backend\test\ILSpy.Host.Tests
dotnet test
popd

pushd backend\src\ILSpy.Host
rmdir ..\..\..\vscode-extension\bin\ilspy-host /s /q
dotnet publish -f net472 -c release -r win7-x64 -o  ..\..\..\vscode-extension\bin\ilspy-host
popd

pushd vscode-extension
call npm install
call npm run compile
call npm test

call npm i vsce -g
call vsce package -o ilspy-vscode-%build_version%.vsix
popd
