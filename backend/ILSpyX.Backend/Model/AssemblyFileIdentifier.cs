namespace ILSpyX.Backend.Model;

public record AssemblyFileIdentifier(string File, string? BundleSubPath = null);

public static class AssemblyFileIdentifierNodeMetadataExtensions
{
    public static AssemblyFileIdentifier GetAssemblyFileIdentifier(this NodeMetadata nodeMetadata)
    {
        return new AssemblyFileIdentifier(nodeMetadata.AssemblyPath, nodeMetadata.BundleSubPath);
    }
}