using ILSpyX.Backend.Decompiler;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata.Ecma335;

namespace ILSpyX.Backend.Tests;

public class TestHelper
{
    public static string AssemblyPath {
        get {
            return Path.Combine(Path.GetDirectoryName(typeof(TestHelper).Assembly.Location) ?? "", "TestAssembly.dll");
        }
    }

    public static string NuGetPackagePath { get; } = Path.Combine(
        (Path.GetDirectoryName(Path.GetDirectoryName(typeof(TestHelper).Assembly.Location)) ?? "").Replace(
            "ILSpyX.Backend.Tests/", "TestAssembly/"),
        "TestAssembly.1.0.0.nupkg");

    public static string NuGetBundledAssemblyName { get; } = "TestAssembly.dll";
    public static string NuGetBundledAssemblyPath { get; } = "lib/net10.0";

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

    public static async Task<int> GetTypeToken(DecompilerBackend decompilerBackend, string @namespace,
        string name)
    {
        return (await decompilerBackend
                .ListTypes(
                    new AssemblyFileIdentifier(TestHelper.AssemblyPath),
                    @namespace))
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }

    public static async Task<int> GetMemberToken(DecompilerBackend decompilerBackend, int parentTypeToken,
        string name)
    {
        return (await decompilerBackend
                .GetMembers(
                    new AssemblyFileIdentifier(TestHelper.AssemblyPath),
                    MetadataTokens.TypeDefinitionHandle(parentTypeToken)))
            .Where(memberData => memberData.Name == name)
            .Select(memberData => memberData.Token)
            .FirstOrDefault();
    }
}