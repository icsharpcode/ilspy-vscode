using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Model;
using System;

namespace ILSpyX.Backend.Decompiler;

public record AssemblyFileIdentifier(string File, string? BundleSubPath = null);

public static class AssemblyFileIdentifierNodeMetadataExtensions
{
    public static AssemblyFileIdentifier GetAssemblyFileIdentifier(this NodeMetadata nodeMetadata)
    {
        return new AssemblyFileIdentifier(nodeMetadata.AssemblyPath, nodeMetadata.BundleSubPath);
    }

    public static AssemblyFileIdentifier GetAssemblyFileIdentifier(this LoadedAssembly loadedAssembly)
    {
        if (loadedAssembly.ParentBundle is not null)
        {
            // TODO Here BundleSubPath will miss the relative path inside of bundle!
            return new AssemblyFileIdentifier(loadedAssembly.ParentBundle.FileName, loadedAssembly.FileName);
        }

        return new AssemblyFileIdentifier(loadedAssembly.FileName);
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