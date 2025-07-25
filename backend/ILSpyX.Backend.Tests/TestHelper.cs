using ILSpyX.Backend.Decompiler;
using Microsoft.Extensions.DependencyInjection;

namespace ILSpyX.Backend.Tests;

public class TestHelper
{
    public static string AssemblyPath {
        get {
            return Path.Combine(Path.GetDirectoryName(typeof(TestHelper).Assembly.Location) ?? "", "TestAssembly.dll");
        }
    }

    public static string NuGetPackagePath {
        get {
            return Path.Combine(
                (Path.GetDirectoryName(Path.GetDirectoryName(typeof(TestHelper).Assembly.Location)) ?? "").Replace(
                    "ILSpyX.Backend.Tests/", "TestAssembly/"),
                "TestAssembly.1.0.0.nupkg");
        }
    }


    public static ILSpyXBackendServices CreateTestServices()
    {
        return new ILSpyXBackendServices();
    }

    public static async Task<ILSpyXBackendServices> CreateTestServicesWithAssembly()
    {
        var services = new ILSpyXBackendServices();
        await services.GetRequiredService<DecompilerBackend>().AddAssemblyAsync(AssemblyPath);
        return services;
    }

    public static async Task<ILSpyXBackendServices> CreateTestServicesWithNuGetPackage()
    {
        var services = new ILSpyXBackendServices();
        string test = NuGetPackagePath;
        await services.GetRequiredService<DecompilerBackend>().AddAssemblyAsync(NuGetPackagePath);
        return services;
    }
}