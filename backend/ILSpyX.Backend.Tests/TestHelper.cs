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
}