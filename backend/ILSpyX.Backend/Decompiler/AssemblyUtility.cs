using ICSharpCode.ILSpyX;
using ILSpyX.Backend.Model;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Decompiler;

public static class AssemblyUtility
{
    public static async Task<AssemblyData?> CreateAssemblyDataAsync(LoadedAssembly loadedAssembly)
    {
        var loadResult = await loadedAssembly.GetLoadResultAsync();
        if (loadResult.MetadataFile is not null)
        {
            var version = loadResult.MetadataFile.Metadata.GetAssemblyDefinition().Version;
            string targetFrameworkId = await loadedAssembly.GetTargetFrameworkIdAsync();
            return new AssemblyData
            {
                Name = loadedAssembly.ShortName,
                FilePath = loadedAssembly.FileName,
                ParentBundleFilePath = loadedAssembly.ParentBundle?.FileName,
                IsAutoLoaded = loadedAssembly.IsAutoLoaded,
                Version = version.ToString(),
                TargetFramework = !string.IsNullOrEmpty(targetFrameworkId)
                    ? targetFrameworkId.Replace("Version=", " ")
                    : null,
                PackageType = PackageType.None
            };
        }

        if (loadResult.Package is not null)
        {
            return new AssemblyData
            {
                Name = loadedAssembly.ShortName,
                FilePath = loadedAssembly.FileName,
                IsAutoLoaded = loadedAssembly.IsAutoLoaded,
                PackageType = loadResult.Package.Kind switch
                {
                    LoadedPackage.PackageKind.Zip => PackageType.NuGet,
                    _ => PackageType.Other
                }
            };
        }

        return null;
    }
}