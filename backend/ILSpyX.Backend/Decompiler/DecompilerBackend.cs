// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Backend.Decompiler;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ILSpy.Backend.Model;
using ILSpyX.Backend.Application;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

class CachedCSharpDecompiler(CSharpDecompiler decompiler, string outputLanguage)
{
    public CSharpDecompiler Decompiler { get; } = decompiler;
    public string OutputLanguage { get; } = outputLanguage;
}

public class DecompilerBackend : IDecompilerBackend
{
    private readonly ILogger logger;
    private readonly Dictionary<string, CachedCSharpDecompiler> decompilers = new();
    private readonly ILSpyBackendSettings ilspyBackendSettings;

    public DecompilerBackend(ILoggerFactory loggerFactory, ILSpyBackendSettings ilspyBackendSettings)
    {
        logger = loggerFactory.CreateLogger<DecompilerBackend>();
        this.ilspyBackendSettings = ilspyBackendSettings;
    }

    public AssemblyData? AddAssembly(string? path)
    {
        if (path is not null)
        {
            try
            {
                return CreateAssemblyData(GetDecompiler(path), path);
            }
            catch (Exception ex)
            {
                logger?.LogError("An exception occurred when reading assembly {assembly}: {exception}", path, ex);
            }
        }

        return null;
    }

    public AssemblyData? CreateAssemblyData(CSharpDecompiler decompiler, string assemblyFile)
    {
        var module = decompiler.TypeSystem.MainModule.MetadataFile;
        var metadata = module.Metadata;
        if (metadata != null)
        {
            AssemblyData assemblyData = new(Path.GetFileNameWithoutExtension(assemblyFile), assemblyFile);
            if (metadata.IsAssembly)
            {
                var assemblyDefinition = metadata.GetAssemblyDefinition();
                assemblyData.Version = assemblyDefinition.Version.ToString();
                var targetFrameworkId = module.DetectTargetFrameworkId();
                if (!string.IsNullOrEmpty(targetFrameworkId))
                {
                    assemblyData.TargetFramework = targetFrameworkId.Replace("Version=", " ");
                }
            }

            return assemblyData;
        }

        return null;
    }

    public bool RemoveAssembly(string? path)
    {
        if (path != null)
        {
            if (decompilers.ContainsKey(path))
            {
                var cachedDecompiler = decompilers[path];
                decompilers.Remove(path);
                DisposeDecompiler(cachedDecompiler.Decompiler);
                return true;
            }
        }

        return false;
    }

    private void DisposeDecompiler(CSharpDecompiler decompiler)
    {
        (decompiler.TypeSystem.MainModule.MetadataFile as PEFile)?.Dispose();
    }

    public CSharpDecompiler GetDecompiler(string assembly, string? outputLanguage = null)
    {
        CSharpDecompiler? decompiler = null;
        if (decompilers.TryGetValue(assembly, out var cachedDecompiler))
        {
            if (outputLanguage is null || cachedDecompiler.OutputLanguage == outputLanguage)
            {
                decompiler = cachedDecompiler.Decompiler;
            }
            else
            {
                DisposeDecompiler(cachedDecompiler.Decompiler);
            }
        }

        if (decompiler is null)
        {
            var assemblyResolver = AssemblyReferences.CreateAssemblyResolver(assembly);
            decompiler = new CSharpDecompiler(
                assembly,
                assemblyResolver,
                ilspyBackendSettings.CreateDecompilerSettings(outputLanguage ?? LanguageName.CSharpLatest));
            decompilers[assembly] = new CachedCSharpDecompiler(decompiler, outputLanguage ?? LanguageName.CSharpLatest);
        }

        return decompiler;
    }

    public IEnumerable<AssemblyData> GetLoadedAssemblies()
    {
        return decompilers.Select(decompiler => CreateAssemblyData(decompiler.Value.Decompiler, decompiler.Key))
            .Where(data => data is not null)
            .Cast<AssemblyData>();
    }

    public AssemblyData GetLoadedAssembly(string assemblyPath)
    {
        var assemblyData = GetLoadedAssemblies()
            .Where(assemblyData => assemblyData.FilePath == assemblyPath).FirstOrDefault();
        if (assemblyData is null)
        {
            throw new InvalidOperationException($"Assembly '{assemblyPath}' not loaded.");
        }
        return assemblyData;
    }

    public IEnumerable<MemberData> GetMembers(string? assemblyPath, TypeDefinitionHandle handle)
    {
        if (handle.IsNil || (assemblyPath == null) || !decompilers.ContainsKey(assemblyPath))
        {
            return Array.Empty<MemberData>();
        }

        var typeSystem = GetDecompiler(assemblyPath).TypeSystem;
        var c = typeSystem.MainModule.GetDefinition(handle);

        return c == null
            ? new List<MemberData>()
            : c.NestedTypes
                .Select(typeDefinition => new MemberData(
                    Name: typeDefinition.TypeToString(includeNamespace: false),
                    Token: MetadataTokens.GetToken(typeDefinition.MetadataToken),
                    SubKind: typeDefinition.Kind))
                .Union(c.Fields.Select(GetMemberData).OrderBy(m => m.Name))
                .Union(c.Properties.Select(GetMemberData).OrderBy(m => m.Name))
                .Union(c.Events.Select(GetMemberData).OrderBy(m => m.Name))
                .Union(c.Methods.Select(GetMemberData).OrderBy(m => m.Name));

        static MemberData GetMemberData(IMember member)
        {
            string memberName = member is IMethod method
                ? method.MethodToString(false, false, false)
                : member.Name;
            return new MemberData(
                Name: memberName,
                Token: MetadataTokens.GetToken(member.MetadataToken),
                SubKind: TypeKind.None);
        }
    }

    public DecompileResult GetCode(string? assemblyPath, EntityHandle handle, string outputLanguage)
    {
        if (assemblyPath is not null)
        {
            return outputLanguage switch
            {
                LanguageName.IL => DecompileResult.WithCode(GetILCode(assemblyPath, handle)),
                _ => DecompileResult.WithCode(GetCSharpCode(assemblyPath, handle, outputLanguage))
            };
        }

        return DecompileResult.WithError("No assembly given");
    }

    private string GetCSharpCode(string assemblyPath, EntityHandle handle, string outputLanguage)
    {
        if (handle.IsNil)
        {
            return string.Empty;
        }

        var decompiler = GetDecompiler(assemblyPath, outputLanguage);
        var module = decompiler.TypeSystem.MainModule;

        switch (handle.Kind)
        {
            case HandleKind.AssemblyDefinition:
                return GetAssemblyCode(assemblyPath, decompiler);
            case HandleKind.TypeDefinition:
                var typeDefinition = module.GetDefinition((TypeDefinitionHandle) handle);
                if (typeDefinition.DeclaringType == null)
                    return decompiler.DecompileTypesAsString(new[] { (TypeDefinitionHandle) handle });
                return decompiler.DecompileAsString(handle);
            case HandleKind.FieldDefinition:
            case HandleKind.MethodDefinition:
            case HandleKind.PropertyDefinition:
            case HandleKind.EventDefinition:
                return decompiler.DecompileAsString(handle);
        }

        return string.Empty;
    }

    private string GetILCode(string assemblyPath, EntityHandle handle)
    {
        if (handle.IsNil)
        {
            return string.Empty;
        }

        var decompiler = GetDecompiler(assemblyPath);
        var module = decompiler.TypeSystem.MainModule;
        var textOutput = new PlainTextOutput();
        var disassembler = CreateDisassembler(assemblyPath, module, textOutput);

        switch (handle.Kind)
        {
            case HandleKind.AssemblyDefinition:
                GetAssemblyILCode(disassembler, assemblyPath, module, textOutput);
                return textOutput.ToString();
            case HandleKind.TypeDefinition:
                disassembler.DisassembleType(module.MetadataFile, (TypeDefinitionHandle) handle);
                return textOutput.ToString();
            case HandleKind.FieldDefinition:
                disassembler.DisassembleField(module.MetadataFile, (FieldDefinitionHandle) handle);
                return textOutput.ToString();
            case HandleKind.MethodDefinition:
                disassembler.DisassembleMethod(module.MetadataFile, (MethodDefinitionHandle) handle);
                return textOutput.ToString();
            case HandleKind.PropertyDefinition:
                disassembler.DisassembleProperty(module.MetadataFile, (PropertyDefinitionHandle) handle);
                return textOutput.ToString();
            case HandleKind.EventDefinition:
                disassembler.DisassembleEvent(module.MetadataFile, (EventDefinitionHandle) handle);
                return textOutput.ToString();
        }

        return string.Empty;
    }

    private static ReflectionDisassembler CreateDisassembler(string assemblyPath, MetadataModule module, ITextOutput textOutput)
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
            targetFramework: module.MetadataFile.DetectTargetFrameworkId());
        dis.AssemblyResolver = resolver;
        dis.DebugInfo = null;

        return dis;
    }

    private static void GetAssemblyILCode(ReflectionDisassembler disassembler, string assemblyPath, MetadataModule module, ITextOutput output)
    {
        output.WriteLine("// " + assemblyPath);
        output.WriteLine();
        var peFile = module.MetadataFile;
        var metadata = peFile.Metadata;

        disassembler.WriteAssemblyReferences(metadata);
        if (metadata.IsAssembly)
        {
            disassembler.WriteAssemblyHeader(peFile);
        }
        output.WriteLine();
        disassembler.WriteModuleHeader(peFile);
    }

    private string GetAssemblyCode(string assemblyPath, CSharpDecompiler decompiler)
    {
        using var output = new StringWriter();
        WriteCommentLine(output, assemblyPath);
        var module = decompiler.TypeSystem.MainModule.MetadataFile;
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
        var corHeader = module.CorHeader;
        if (corHeader != null)
        {
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
            if (module is PEFile peFileModule)
            {
                output.WriteLine("// Architecture: " + peFileModule.GetPlatformDisplayName());
            }
            if ((corHeader.Flags & System.Reflection.PortableExecutable.CorFlags.ILOnly) == 0)
            {
                output.WriteLine("// This assembly contains unmanaged code.");
            }
        }
        if (module is PEFile peFile)
        {
            string runtimeName = peFile.GetRuntimeDisplayName();
            if (runtimeName != null)
            {
                output.WriteLine("// Runtime: " + runtimeName);
            }
        }
        output.WriteLine();

        output.Write(decompiler.DecompileModuleAndAssemblyAttributesToString());

        output.WriteLine();

        return output.ToString();
    }

    private static void WriteCommentLine(StringWriter output, string s)
    {
        output.WriteLine($"// {s}");
    }

    public IEnumerable<MemberData> ListTypes(string? assemblyPath, string? @namespace)
    {
        if ((assemblyPath == null) || (@namespace == null) || !decompilers.ContainsKey(assemblyPath))
        {
            yield break;
        }

        var decompiler = GetDecompiler(assemblyPath);
        var currentNamespace = decompiler.TypeSystem.MainModule.RootNamespace;
        string[] parts = @namespace.Split('.');

        if (!(parts.Length == 1 && string.IsNullOrEmpty(parts[0])))
        {
            // not the global namespace
            foreach (var part in parts)
            {
                var nested = currentNamespace.GetChildNamespace(part);
                if (nested == null)
                    yield break;
                currentNamespace = nested;
            }
        }

        foreach (var t in currentNamespace.Types.OrderBy(t => t.FullName))
        {
            yield return new MemberData(
                Name: t.TypeToString(includeNamespace: false),
                Token: MetadataTokens.GetToken(t.MetadataToken),
                SubKind: t.Kind);
        }
    }
}
