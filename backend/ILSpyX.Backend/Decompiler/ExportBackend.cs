using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Application;
using ILSpyX.Backend.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Decompiler;

public class ExportBackend(
    ILoggerFactory loggerFactory,
    ILSpyBackendSettings ilspyBackendSettings,
    DecompilerBackend decompilerBackend,
    SingleThreadAssemblyList assemblyList)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<ExportBackend>();

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
            logger.LogError(
                ex,
                "Failed to create output directory {outputDirectory}",
                outputDirectory);
            return new ExportAssemblyResult(false, outputDirectory, 0, 1, ex.Message);
        }

        string assemblyName = nodeMetadata.Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assemblyName = Path.GetFileName(assemblyFile.BundledAssemblyFile ?? assemblyFile.File);
        }

        string baseName = Path.GetFileNameWithoutExtension(assemblyName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "assembly";
        }

        string targetDirectory = CreateUniqueDirectory(outputDirectory, baseName);

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
            logger.LogError(
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

        var resolver = loadedAssembly.GetAssemblyResolver();
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
        var settings = ilspyBackendSettings.CreateDecompilerSettings();
        settings.UseNestedDirectoriesForNamespaces = true;

        var resolver = loadedAssembly.GetAssemblyResolver();
        var typeSystem = includeCompilerGenerated
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
        string assemblyInfoPath = Path.Combine(
            targetDirectory,
            WholeProjectDecompiler.CleanUpFileName("AssemblyInfo", ".il"));
        var assemblyInfoResult = await decompilerBackend.GetCode(
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

            var result = await decompilerBackend.GetCode(
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
            : base(settings, assemblyResolver, projectWriter: null, assemblyReferenceClassifier: null,
                debugInfoProvider: null)
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