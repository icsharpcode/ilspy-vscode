// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.Extensions.Logging;
using OmniSharp.Host.Services;

namespace ILSpy.Host.Providers
{
    public class SimpleDecompilationProvider : IDecompilationProvider
    {
        private ILogger _logger;
        private Dictionary<string, CSharpDecompiler> _decompilers = new Dictionary<string, CSharpDecompiler>();

        public SimpleDecompilationProvider(IMsilDecompilerEnvironment decompilationConfiguration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SimpleDecompilationProvider>();
        }

        public bool AddAssembly(string path)
        {
            try
            {
                var decompiler = new CSharpDecompiler(path, new DecompilerSettings() { ThrowOnAssemblyResolveErrors = false });
                _decompilers[path] = decompiler;
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError("An exception occurred when reading assembly {assembly}: {exception}", path, ex);
            }

            return false;
        }

        public IEnumerable<MemberData> GetMembers(string assemblyPath, TypeDefinitionHandle handle)
        {
            if (handle.IsNil)
                return Array.Empty<MemberData>();

            var typeSystem = _decompilers[assemblyPath].TypeSystem;
            var c = typeSystem.MainModule.GetDefinition(handle);

            return c == null
                ? new List<MemberData>()
                : c.NestedTypes.Select(typeDefinition => new MemberData
                    {
                        Name = typeDefinition.TypeToString(includeNamespace: false),
                        Token = MetadataTokens.GetToken(typeDefinition.MetadataToken),
                        MemberSubKind = typeDefinition.Kind
                    })
                    .Union(c.Fields.Select(GetMemberData))
                    .Union(c.Properties.Select(GetMemberData))
                    .Union(c.Events.Select(GetMemberData))
                    .Union(c.Methods.Select(GetMemberData));

            MemberData GetMemberData(IMember member)
            {
                string memberName = member is IMethod method
                    ? method.MethodToString(false, false, false)
                    : member.Name;
                return new MemberData
                {
                    Name = memberName,
                    Token = MetadataTokens.GetToken(member.MetadataToken),
                    MemberSubKind = TypeKind.None
                };
            }
        }

        public string GetCSharpCode(string assemblyPath, EntityHandle handle)
        {
            if (handle.IsNil)
                return string.Empty;

            var dc = _decompilers[assemblyPath];
            var module = dc.TypeSystem.MainModule;

            switch (handle.Kind)
            {
                case HandleKind.AssemblyDefinition:
                    return GetAssemblyCode(assemblyPath, dc);
                case HandleKind.TypeDefinition:
                    var td = module.GetDefinition((TypeDefinitionHandle)handle);
                    if (td.DeclaringType == null)
                        return dc.DecompileTypesAsString(new[] { (TypeDefinitionHandle)handle });
                    return dc.DecompileAsString(handle);
                case HandleKind.FieldDefinition:
                case HandleKind.MethodDefinition:
                case HandleKind.PropertyDefinition:
                case HandleKind.EventDefinition:
                    return dc.DecompileAsString(handle);
            }

            return string.Empty;
        }

        public string GetILCode(string assemblyPath, EntityHandle handle)
        {
            if (handle.IsNil)
                return string.Empty;

            var dc = _decompilers[assemblyPath];
            var module = dc.TypeSystem.MainModule;
            var textOutput = new PlainTextOutput();
            var disassembler = CreateDisassembler(assemblyPath, module, textOutput);

            switch (handle.Kind)
            {
                case HandleKind.AssemblyDefinition:
                    return "TODO: assembly IL";
                case HandleKind.TypeDefinition:
                    disassembler.DisassembleType(module.PEFile, (TypeDefinitionHandle)handle);
                    return textOutput.ToString();
                case HandleKind.FieldDefinition:
                    var dis = CreateDisassembler(assemblyPath, module, textOutput);
                    disassembler.DisassembleField(module.PEFile, (FieldDefinitionHandle)handle);
                    return textOutput.ToString();
                case HandleKind.MethodDefinition:
                    disassembler.DisassembleMethod(module.PEFile, (MethodDefinitionHandle)handle);
                    return textOutput.ToString();
                case HandleKind.PropertyDefinition:
                    disassembler.DisassembleProperty(module.PEFile, (PropertyDefinitionHandle)handle);
                    return textOutput.ToString();
                case HandleKind.EventDefinition:
                    disassembler.DisassembleEvent(module.PEFile, (EventDefinitionHandle)handle);
                    return textOutput.ToString();
            }

            return string.Empty;
        }

        private static ReflectionDisassembler CreateDisassembler(string assemblyPath, MetadataModule module, PlainTextOutput textOutput)
        {
            var dis = new ReflectionDisassembler(textOutput, CancellationToken.None)
            {
                DetectControlStructure = true,
                ShowSequencePoints = false,
                ShowMetadataTokens = true,
                ExpandMemberDefinitions = true,
            };
            var resolver = new UniversalAssemblyResolver(assemblyPath,
                throwOnError: true,
                targetFramework: module.PEFile.Reader.DetectTargetFrameworkId());
            dis.AssemblyResolver = resolver;
            dis.DebugInfo = null;

            return dis;
        }

        private string GetAssemblyCode(string assemblyPath, CSharpDecompiler decompiler)
        {
            using (var output = new StringWriter())
            {
                WriteCommentLine(output, assemblyPath);
                var module = decompiler.TypeSystem.MainModule.PEFile;
                var metadata = module.Metadata;
                if (metadata.IsAssembly)
                {
                    var name = metadata.GetAssemblyDefinition();
                    if ((name.Flags & System.Reflection.AssemblyFlags.WindowsRuntime) != 0)
                    {
                        WriteCommentLine(output, metadata.GetString(name.Name) + " [WinRT]");
                    }
                    else
                    {
                        WriteCommentLine(output, metadata.GetFullAssemblyName());
                    }
                }
                else
                {
                    WriteCommentLine(output, module.Name);
                }

                var mainModule = decompiler.TypeSystem.MainModule;
                var globalType = mainModule.TypeDefinitions.FirstOrDefault();
                if (globalType != null)
                {
                    output.Write("// Global type: ");
                    output.Write(globalType.FullName);
                    output.WriteLine();
                }
                var corHeader = module.Reader.PEHeaders.CorHeader;
                var entrypointHandle = MetadataTokenHelpers.EntityHandleOrNil(corHeader.EntryPointTokenOrRelativeVirtualAddress);
                if (!entrypointHandle.IsNil && entrypointHandle.Kind == HandleKind.MethodDefinition)
                {
                    var entrypoint = mainModule.ResolveMethod(entrypointHandle, new ICSharpCode.Decompiler.TypeSystem.GenericContext());
                    if (entrypoint != null)
                    {
                        output.Write("// Entry point: ");
                        output.Write(entrypoint.DeclaringType.FullName + "." + entrypoint.Name);
                        output.WriteLine();
                    }
                }
                output.WriteLine("// Architecture: " + module.GetPlatformDisplayName());
                if ((corHeader.Flags & System.Reflection.PortableExecutable.CorFlags.ILOnly) == 0)
                {
                    output.WriteLine("// This assembly contains unmanaged code.");
                }
                string runtimeName = module.GetRuntimeDisplayName();
                if (runtimeName != null)
                {
                    output.WriteLine("// Runtime: " + runtimeName);
                }
                output.WriteLine();

                output.Write(decompiler.DecompileModuleAndAssemblyAttributesToString());

                output.WriteLine();

                return output.ToString();
            }
        }

        private static void WriteCommentLine(StringWriter output, string s)
        {
            output.WriteLine($"// {s}");
        }

        public IEnumerable<MemberData> ListTypes(string assemblyPath, string @namespace)
        {
            var decompiler = _decompilers[assemblyPath];
            var currentNamespace = decompiler.TypeSystem.MainModule.RootNamespace;
            string[] parts = @namespace.Split('.');

            foreach (var part in parts)
            {
                var nested = currentNamespace.GetChildNamespace(part);
                if (nested == null)
                    yield break;
                currentNamespace = nested;
            }

            foreach (var t in currentNamespace.Types)
            {
                yield return new MemberData
                {
                    Name = t.TypeToString(includeNamespace: false),
                    Token = MetadataTokens.GetToken(t.MetadataToken),
                    MemberSubKind = t.Kind
                };
            }
        }

        public IEnumerable<string> ListNamespaces(string assemblyPath)
        {
            var decompiler = _decompilers[assemblyPath];
            var types = decompiler.TypeSystem.MainModule.TopLevelTypeDefinitions;
            HashSet<string> namespaces = new HashSet<string>(decompiler.TypeSystem.NameComparer);
            foreach (var type in types)
            {
                namespaces.Add(type.Namespace);
            }
            return namespaces.OrderBy(n => n);
        }
    }
}
