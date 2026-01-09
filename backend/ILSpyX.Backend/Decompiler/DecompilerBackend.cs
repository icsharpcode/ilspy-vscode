// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
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
using System.Text;
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
        WriteCommentLine(output);

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

    public async Task<ExportAssemblyResult> ExportAssemblyAsync(
        NodeMetadata nodeMetadata,
        string outputLanguage,
        string outputDirectory,
        bool includeCompilerGenerated,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return new ExportAssemblyResult(false, null, 0, 1, "Output directory is required.");
        }

        var assemblyFile = nodeMetadata.GetAssemblyFileIdentifier();
        var loadedAssembly = await assemblyList.FindAssembly(assemblyFile);
        if (loadedAssembly is null)
        {
            return new ExportAssemblyResult(false, null, 0, 1, "Assembly is not loaded.");
        }

        var metadataFile = await loadedAssembly.GetMetadataFileOrNullAsync();
        if (metadataFile is null)
        {
            return new ExportAssemblyResult(false, null, 0, 1, "Assembly metadata could not be loaded.");
        }

        try
        {
            Directory.CreateDirectory(outputDirectory);
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to create output directory {outputDirectory}",
                outputDirectory);
            return new ExportAssemblyResult(false, outputDirectory, 0, 1, ex.Message);
        }

        var assemblyName = nodeMetadata.Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assemblyName = Path.GetFileName(assemblyFile.BundledAssemblyFile ?? assemblyFile.File);
        }

        var baseName = Path.GetFileNameWithoutExtension(assemblyName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "assembly";
        }

        var targetDirectory = CreateUniqueDirectory(outputDirectory, baseName);

        try
        {
            return outputLanguage == LanguageName.IL
                ? await ExportIlAsync(
                    metadataFile,
                    loadedAssembly,
                    assemblyFile,
                    targetDirectory,
                    includeCompilerGenerated,
                    cancellationToken)
                : await ExportCSharpAsync(
                    metadataFile,
                    loadedAssembly,
                    targetDirectory,
                    outputLanguage,
                    includeCompilerGenerated,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to export assembly {assembly} to {outputDirectory}",
                assemblyName,
                targetDirectory);
            return new ExportAssemblyResult(false, targetDirectory, 0, 1, ex.Message);
        }
    }

    private async Task<ExportAssemblyResult> ExportCSharpAsync(
        MetadataFile metadataFile,
        LoadedAssembly loadedAssembly,
        string targetDirectory,
        string outputLanguage,
        bool includeCompilerGenerated,
        CancellationToken cancellationToken)
    {
        var settings = ilspyBackendSettings.CreateDecompilerSettings(outputLanguage);
        settings.UseNestedDirectoriesForNamespaces = true;

        var resolver = loadedAssembly.GetAssemblyResolver(true);
        var projectDecompiler = new SimpleProjectDecompiler(
            settings,
            resolver,
            metadataFile,
            includeCompilerGenerated);
        await Task.Run(
            () => projectDecompiler.DecompileProject(metadataFile, targetDirectory, cancellationToken),
            cancellationToken);

        return new ExportAssemblyResult(
            true,
            targetDirectory,
            projectDecompiler.FilesWritten,
            0,
            null);
    }

    private async Task<ExportAssemblyResult> ExportIlAsync(
        MetadataFile metadataFile,
        LoadedAssembly loadedAssembly,
        AssemblyFileIdentifier assemblyFile,
        string targetDirectory,
        bool includeCompilerGenerated,
        CancellationToken cancellationToken)
    {
        var settings = ilspyBackendSettings.CreateDecompilerSettings(LanguageName.CSharpLatest);
        settings.UseNestedDirectoriesForNamespaces = true;

        var resolver = loadedAssembly.GetAssemblyResolver(true);
        DecompilerTypeSystem? typeSystem = includeCompilerGenerated
            ? null
            : new DecompilerTypeSystem(metadataFile, resolver, settings);

        var metadata = metadataFile.Metadata;
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var usedPaths = new HashSet<string>(comparer);
        int filesWritten = 0;
        int errorCount = 0;

        cancellationToken.ThrowIfCancellationRequested();
        var assemblyInfoPath = Path.Combine(
            targetDirectory,
            WholeProjectDecompiler.CleanUpFileName("AssemblyInfo", ".il"));
        var assemblyInfoResult = await GetCode(
            assemblyFile,
            EntityHandle.AssemblyDefinition,
            LanguageName.IL);
        await WriteTextAsync(
            assemblyInfoPath,
            BuildExportContent(assemblyInfoResult, "assembly"),
            cancellationToken);
        usedPaths.Add(assemblyInfoPath);
        filesWritten++;
        if (assemblyInfoResult.IsError)
        {
            errorCount++;
        }

        foreach (var typeHandle in metadata.GetTopLevelTypeDefinitions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ShouldExportType(metadataFile, typeHandle, settings, includeCompilerGenerated, typeSystem))
            {
                continue;
            }

            var typeDef = metadata.GetTypeDefinition(typeHandle);
            string name = metadata.GetString(typeDef.Name);
            string ns = metadata.GetString(typeDef.Namespace);
            string directoryPath = string.IsNullOrEmpty(ns)
                ? targetDirectory
                : Path.Combine(
                    targetDirectory,
                    settings.UseNestedDirectoriesForNamespaces
                        ? WholeProjectDecompiler.CleanUpPath(ns)
                        : WholeProjectDecompiler.CleanUpDirectoryName(ns));
            Directory.CreateDirectory(directoryPath);

            int symbolToken = MetadataTokens.GetToken(typeHandle);
            string filePath = GetUniqueOutputPath(
                directoryPath,
                name,
                ".il",
                symbolToken,
                usedPaths);

            var result = await GetCode(
                assemblyFile,
                MetadataTokens.EntityHandle(symbolToken),
                LanguageName.IL);
            await WriteTextAsync(
                filePath,
                BuildExportContent(result, name),
                cancellationToken);
            filesWritten++;
            if (result.IsError)
            {
                errorCount++;
            }
        }

        return new ExportAssemblyResult(true, targetDirectory, filesWritten, errorCount, null);
    }

    private static bool ShouldExportType(
        MetadataFile metadataFile,
        TypeDefinitionHandle typeHandle,
        DecompilerSettings settings,
        bool includeCompilerGenerated,
        DecompilerTypeSystem? typeSystem)
    {
        var metadata = metadataFile.Metadata;
        var typeDef = metadata.GetTypeDefinition(typeHandle);
        string name = metadata.GetString(typeDef.Name);
        string ns = metadata.GetString(typeDef.Namespace);
        if (name == "<Module>" || CSharpDecompiler.MemberIsHidden(metadataFile, typeHandle, settings))
        {
            return false;
        }

        if (ns == "XamlGeneratedNamespace" && name == "GeneratedInternalTypeHelper")
        {
            return false;
        }

        if (!includeCompilerGenerated && typeSystem is not null)
        {
            var definition = typeSystem.MainModule.GetDefinition(typeHandle);
            if (definition?.IsCompilerGenerated() == true)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetUniqueOutputPath(
        string directoryPath,
        string baseName,
        string extension,
        int symbolToken,
        HashSet<string> usedPaths)
    {
        string baseFileName = WholeProjectDecompiler.CleanUpFileName(baseName, extension);
        string basePath = Path.Combine(directoryPath, baseFileName);
        if (usedPaths.Add(basePath))
        {
            return basePath;
        }

        string suffixBase = $"{baseName}_{symbolToken}";
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            string suffix = attempt == 0 ? suffixBase : $"{suffixBase}_{attempt}";
            string fileName = WholeProjectDecompiler.CleanUpFileName(suffix, extension);
            string candidate = Path.Combine(directoryPath, fileName);
            if (usedPaths.Add(candidate))
            {
                return candidate;
            }
        }

        string fallbackName = WholeProjectDecompiler.CleanUpFileName(
            $"{baseName}_{symbolToken}_{DateTime.UtcNow.Ticks}",
            extension);
        string fallbackPath = Path.Combine(directoryPath, fallbackName);
        usedPaths.Add(fallbackPath);
        return fallbackPath;
    }

    private static async Task WriteTextAsync(
        string path,
        string contents,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(path, contents, Encoding.UTF8, cancellationToken);
    }

    private static string BuildExportContent(DecompileResult result, string label)
    {
        if (!string.IsNullOrEmpty(result.DecompiledCode))
        {
            return EnsureTrailingNewline(result.DecompiledCode);
        }

        string error = result.ErrorMessage ?? "Unknown error.";
        return EnsureTrailingNewline($"// Failed to decompile {label}.\n// {error}\n");
    }

    private static string EnsureTrailingNewline(string content)
    {
        return content.EndsWith('\n') ? content : $"{content}\n";
    }

    private static string CreateUniqueDirectory(string baseDirectory, string folderName)
    {
        string safeName = WholeProjectDecompiler.CleanUpDirectoryName(folderName);
        string candidate = Path.Combine(baseDirectory, safeName);
        if (!Directory.Exists(candidate) && !File.Exists(candidate))
        {
            Directory.CreateDirectory(candidate);
            return candidate;
        }

        for (int i = 1; i < 1000; i++)
        {
            string next = Path.Combine(baseDirectory, $"{safeName}-{i}");
            if (!Directory.Exists(next) && !File.Exists(next))
            {
                Directory.CreateDirectory(next);
                return next;
            }
        }

        Directory.CreateDirectory(candidate);
        return candidate;
    }

    private sealed class SimpleProjectDecompiler : WholeProjectDecompiler
    {
        private readonly bool includeCompilerGenerated;
        private readonly DecompilerTypeSystem? typeSystem;
        private int filesWritten;

        public int FilesWritten => filesWritten;

        public SimpleProjectDecompiler(
            DecompilerSettings settings,
            IAssemblyResolver assemblyResolver,
            MetadataFile metadataFile,
            bool includeCompilerGenerated)
            : base(settings, assemblyResolver, projectWriter: null, assemblyReferenceClassifier: null, debugInfoProvider: null)
        {
            this.includeCompilerGenerated = includeCompilerGenerated;
            if (!includeCompilerGenerated)
            {
                typeSystem = new DecompilerTypeSystem(metadataFile, assemblyResolver, settings);
            }
        }

        protected override bool IncludeTypeWhenDecompilingProject(MetadataFile module, TypeDefinitionHandle type)
        {
            if (!base.IncludeTypeWhenDecompilingProject(module, type))
            {
                return false;
            }

            if (includeCompilerGenerated || typeSystem is null)
            {
                return true;
            }

            var definition = typeSystem.MainModule.GetDefinition(type);
            return definition?.IsCompilerGenerated() != true;
        }

        protected override IEnumerable<ProjectItemInfo> WriteResourceFilesInProject(MetadataFile module)
        {
            return [];
        }

        protected override IEnumerable<ProjectItemInfo> WriteMiscellaneousFilesInProject(PEFile module)
        {
            return [];
        }

        protected override TextWriter CreateFile(string path)
        {
            Interlocked.Increment(ref filesWritten);
            return base.CreateFile(path);
        }
    }
}
