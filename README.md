# MSIL decompilation back end

## ILSpy.Host

A back end service to provide MSIL decompilations using ILSpy API.

```
Usage:  [options]

Options:
  -? | -h | --help   Show help information
  -a | --assembly    Path to the managed assembly to decompile
  -p | --port        MsilDecompiler port (defaults to 2000).
  -l | --loglevel    Level of logging (defaults to 'Information').
  -v | --verbose     Explicitly set 'Debug' log level.
  -hpid | --hostPID  Host process ID.
  -stdio | --stdio   Use STDIO over HTTP as MsilDecompiler communication protocol.
  -e | --encoding    Input / output encoding for STDIO protocol.
  -i | --interface   Server interface address (defaults to 'localhost').
```

Supported commands are of the following format

```javascript
{"Seq":1,"Command":"/addassembly",Arguments:{"AssemblyPath":"E:\\temp\\TestAssembly.dll"}}
{"Seq":2,"Command":"/decompileassembly",Arguments:{"AssemblyPath":"E:\\temp\\TestAssembly.dll"}}
{"Seq":3,"Command":"/listtypes",Arguments:{"AssemblyPath":"E:\\temp\\TestAssembly.dll","Namespace":"TestAssembly"}}
{"Seq":4,"Command":"/decompiletype",Arguments:{"AssemblyPath":"E:\\temp\\TestAssembly.dll","Rid":2}}
{"Seq":5,"Command":"/listmembers",Arguments:{"AssemblyPath":"E:\\temp\\TestAssembly.dll","Rid":2}}
{"Seq":6,"Command":"/decompilemember",Arguments:{"AssemblyPath":"E:\\temp\\TestAssembly.dll","TypeRid":2,"MemberType":100663296,"MemberRid":3}}
```

## Develop

To build the solution,

```
git clone <REPO_URL>
git submodule init
git submodule update
msbuild MsilDecompiler.sln
```
## License

[MIT license](LICENSE.TXT).
