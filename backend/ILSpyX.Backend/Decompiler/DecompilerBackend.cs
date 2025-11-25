// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Application;
using ILSpyX.Backend.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Decompiler;

public class DecompilerBackend(
    ILoggerFactory loggerFactory,
    ILSpyBackendSettings ilspyBackendSettings,
    SingleThreadAssemblyList assemblyList)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<DecompilerBackend>();

    public async Task<AssemblyData?> AddAssemblyAsync(string? path)
    {
        if (path is null)
        {
            return null;
        }

        try
        {
            var loadedAssembly = await assemblyList.AddAssembly(path);
            if (loadedAssembly is not null)
            {
                return await AssemblyUtility.CreateAssemblyDataAsync(loadedAssembly);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError("An exception occurred when reading assembly {assembly}: {exception}", path, ex);
        }

        return null;
    }

    public async Task<bool> RemoveAssemblyAsync(string? path)
    {
        if (path is null)
        {
            return false;
        }

        await assemblyList.RemoveAssembly(path);
        return true;
    }

    public async Task<(T Result, bool NewAutoLoadedAssemblies)> DetectAutoLoadedAssemblies<T>(Func<Task<T>> operation)
    {
        var autoLoadedAssembliesBefore = (await GetLoadedAssembliesAsync()).Where(assembly => assembly.IsAutoLoaded);
        var result = await operation();
        var autoLoadedAssembliesAfter = (await GetLoadedAssembliesAsync()).Where(assembly => assembly.IsAutoLoaded);
        return (result, autoLoadedAssembliesBefore.Count() != autoLoadedAssembliesAfter.Count());
    }

    public async Task<CSharpDecompiler?> CreateDecompiler(AssemblyFileIdentifier assemblyFile,
        string? outputLanguage = null)
    {
        var loadedAssembly = await assemblyList.FindAssembly(assemblyFile);
        if (loadedAssembly is null)
        {
            return null;
        }

        var metadataFile = await loadedAssembly.GetMetadataFileOrNullAsync();
        if (metadataFile is not null)
        {
            return new CSharpDecompiler(
                metadataFile,
                loadedAssembly.GetAssemblyResolver(true),
                ilspyBackendSettings.CreateDecompilerSettings(outputLanguage ?? LanguageName.CSharpLatest));
        }

        return null;
    }

    public async Task<IEnumerable<AssemblyData>> GetLoadedAssembliesAsync()
    {
        return (await Task.WhenAll(
                assemblyList.GetLoadedAssemblies()
                    .Select(async loadedAssembly => await AssemblyUtility.CreateAssemblyDataAsync(loadedAssembly))))
            .Where(data => data is not null)
            .Cast<AssemblyData>();
    }

    public async Task<IEnumerable<MemberData>> GetMembers(AssemblyFileIdentifier assemblyFile,
        TypeDefinitionHandle handle)
    {
        if (handle.IsNil)
        {
            return [];
        }

        var loadedAssembly = assemblyList.FindAssembly(assemblyFile.File);
        if (loadedAssembly is null)
        {
            return [];
        }

        var decompiler = await CreateDecompiler(assemblyFile);
        if (decompiler is null)
        {
            return [];
        }

        var typeSystem = decompiler.TypeSystem;
        var definition = typeSystem.MainModule.GetDefinition(handle);

        return definition == null
            ? new List<MemberData>()
            : definition.NestedTypes
                .Select(typeDefinition => new MemberData(
                    Name: typeDefinition.TypeToString(includeNamespace: false),
                    Token: MetadataTokens.GetToken(typeDefinition.MetadataToken),
                    SubKind: typeDefinition.Kind))
                .Union(definition.Fields.Select(GetMemberData).OrderBy(m => m.Name))
                .Union(definition.Properties.Select(GetMemberData).OrderBy(m => m.Name))
                .Union(definition.Events.Select(GetMemberData).OrderBy(m => m.Name))
                .Union(definition.Methods.Select(GetMemberData).OrderBy(m => m.Name));

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

    public async Task<DecompileResult> GetCode(AssemblyFileIdentifier assemblyFile, EntityHandle handle,
        string outputLanguage)
    {
        return outputLanguage switch
        {
            LanguageName.IL => DecompileResult.WithCode(await GetILCode(assemblyFile, handle)),
            _ => DecompileResult.WithCode(await GetCSharpCode(assemblyFile, handle, outputLanguage))
        };
    }

    public async Task<IEntity?> GetEntityFromHandle(AssemblyFileIdentifier assemblyFile, EntityHandle handle)
    {
        if (!handle.IsNil)
        {
            var decompiler = await CreateDecompiler(assemblyFile);
            if (decompiler is not null)
            {
                var module = decompiler.TypeSystem.MainModule;
                return handle.Kind switch
                {
                    HandleKind.TypeDefinition => module.GetDefinition((TypeDefinitionHandle) handle),
                    HandleKind.FieldDefinition => module.GetDefinition((FieldDefinitionHandle) handle),
                    HandleKind.MethodDefinition => module.GetDefinition((MethodDefinitionHandle) handle),
                    HandleKind.PropertyDefinition => module.GetDefinition((PropertyDefinitionHandle) handle),
                    HandleKind.EventDefinition => module.GetDefinition((EventDefinitionHandle) handle),
                    _ => null,
                };
            }
        }

        return null;
    }

    private async Task<string> GetCSharpCode(AssemblyFileIdentifier assemblyFile, EntityHandle handle,
        string outputLanguage)
    {
        if (handle.IsNil)
        {
            return string.Empty;
        }

        var decompiler = await CreateDecompiler(assemblyFile, outputLanguage);
        if (decompiler is null)
        {
            return string.Empty;
        }

        var module = decompiler.TypeSystem.MainModule;

        switch (handle.Kind)
        {
            case HandleKind.AssemblyDefinition:
                return GetAssemblyCode(assemblyFile, decompiler);
            case HandleKind.TypeDefinition:
                var typeDefinition = module.GetDefinition((TypeDefinitionHandle) handle);
                return typeDefinition.DeclaringType == null
                    ? decompiler.DecompileTypesAsString([(TypeDefinitionHandle) handle])
                    : decompiler.DecompileAsString(handle);

            case HandleKind.FieldDefinition:
            case HandleKind.MethodDefinition:
            case HandleKind.PropertyDefinition:
            case HandleKind.EventDefinition:
                return decompiler.DecompileAsString(handle);
        }

        return string.Empty;
    }

    private async Task<string> GetILCode(AssemblyFileIdentifier assemblyFile, EntityHandle handle)
    {
        if (handle.IsNil)
        {
            return string.Empty;
        }

        var decompiler = await CreateDecompiler(assemblyFile);
        if (decompiler is null)
        {
            return string.Empty;
        }

        var module = decompiler.TypeSystem.MainModule;
        var textOutput = new PlainTextOutput();
        var disassembler = CreateDisassembler(assemblyFile.File, module, textOutput);

        switch (handle.Kind)
        {
            case HandleKind.AssemblyDefinition:
                GetAssemblyILCode(disassembler, assemblyFile.File, module, textOutput);
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

    private static ReflectionDisassembler CreateDisassembler(string assemblyPath, MetadataModule module,
        ITextOutput textOutput)
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

    private static void GetAssemblyILCode(ReflectionDisassembler disassembler, string assemblyPath,
        MetadataModule module, ITextOutput output)
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

    private string GetAssemblyCode(AssemblyFileIdentifier assemblyFile, CSharpDecompiler decompiler)
    {
        using var output = new StringWriter();
        WriteCommentLine(output, assemblyFile.BundledAssemblyFile ?? assemblyFile.File);
        var module = decompiler.TypeSystem.MainModule.MetadataFile;
        if (module is null)
        {
            return string.Empty;
        }

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
            var entrypointHandle =
                MetadataTokenHelpers.EntityHandleOrNil(corHeader.EntryPointTokenOrRelativeVirtualAddress);
            if (!entrypointHandle.IsNil && entrypointHandle.Kind == HandleKind.MethodDefinition)
            {
                var entrypoint = mainModule.ResolveMethod(entrypointHandle,
                    new ICSharpCode.Decompiler.TypeSystem.GenericContext());
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
            output.WriteLine("// Runtime: " + runtimeName);
        }

        output.WriteLine();

        output.Write(decompiler.DecompileModuleAndAssemblyAttributesToString());

        output.WriteLine();

        return output.ToString();
    }

    public async Task<DecompileResult> GetRootPackageCode(AssemblyFileIdentifier packageFile)
    {
        await using var output = new StringWriter();
        WriteCommentLine(output, packageFile.File);

        var loadedAssembly = await assemblyList.FindAssembly(packageFile);
        if (loadedAssembly is not null)
        {
            var package = (await loadedAssembly.GetLoadResultAsync()).Package;
            if (package is not null)
            {
                switch (package.Kind)
                {
                    case LoadedPackage.PackageKind.Zip:
                        WriteCommentLine(output, "File format: .zip file");
                        break;
                    case LoadedPackage.PackageKind.Bundle:
                        var header = package.BundleHeader;
                        WriteCommentLine(output,
                            $"File format: .NET bundle {header.MajorVersion}.{header.MinorVersion}");
                        break;
                }

                WriteCommentLine(output);
                WriteCommentLine(output, "Entries:");
                foreach (var entry in package.Entries)
                {
                    WriteCommentLine(output, $" {entry.Name} ({entry.TryGetLength()} bytes)");
                }
            }
        }

        return DecompileResult.WithCode(output.ToString());
    }

    private static void WriteCommentLine(StringWriter output, string? s = null)
    {
        if (s is not null)
        {
            output.WriteLine($"// {s}");
        }
        else
        {
            output.WriteLine();
        }
    }

    public async Task<IEnumerable<MemberData>> ListTypes(AssemblyFileIdentifier assemblyFile, string? @namespace)
    {
        if (@namespace == null)
        {
            return [];
        }

        var loadedAssembly = await assemblyList.FindAssembly(assemblyFile);
        if (loadedAssembly is null)
        {
            return [];
        }

        var decompiler = await CreateDecompiler(assemblyFile);
        if (decompiler is null)
        {
            return [];
        }

        var currentNamespace = decompiler.TypeSystem.MainModule.RootNamespace;
        string[] parts = @namespace.Split('.');

        if (!(parts.Length == 1 && string.IsNullOrEmpty(parts[0])))
        {
            // not the global namespace
            foreach (string part in parts)
            {
                var nested = currentNamespace.GetChildNamespace(part);
                if (nested == null)
                {
                    break;
                }

                currentNamespace = nested;
            }
        }

        return currentNamespace.Types.OrderBy(t => t.FullName).Select(t =>
            new MemberData(Name: t.TypeToString(includeNamespace: false),
                Token: MetadataTokens.GetToken(t.MetadataToken), SubKind: t.Kind));
    }
}