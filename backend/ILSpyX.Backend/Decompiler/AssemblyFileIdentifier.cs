using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Model;
using System;

namespace ILSpyX.Backend.Decompiler;

public record AssemblyFileIdentifier(string File, string? BundledAssemblyFile = null);

public static class AssemblyFileIdentifierNodeMetadataExtensions
{
    public static AssemblyFileIdentifier GetAssemblyFileIdentifier(this NodeMetadata nodeMetadata)
    {
        return new AssemblyFileIdentifier(nodeMetadata.AssemblyPath, nodeMetadata.BundledAssemblyName);
    }

    public static AssemblyFileIdentifier GetAssemblyFileIdentifier(this LoadedAssembly loadedAssembly)
    {
        return loadedAssembly.ParentBundle is not null
            ? new AssemblyFileIdentifier(loadedAssembly.ParentBundle.FileName, loadedAssembly.FileName)
            : new AssemblyFileIdentifier(loadedAssembly.FileName);
    }

    public static AssemblyFileIdentifier? GetAssemblyFileIdentifier(this MetadataFile metadataFile)
    {
        try
        {
            return metadataFile?.GetLoadedAssembly()?.GetAssemblyFileIdentifier();
        }
        catch (Exception)
        {
            return null;
        }
    }
}