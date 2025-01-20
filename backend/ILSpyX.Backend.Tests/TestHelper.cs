using ILSpyX.Backend.Application;
using Microsoft.Extensions.Logging.Abstractions;

namespace ILSpyX.Backend.Tests;

public class TestHelper
{
    public static string AssemblyPath => Path.Combine(Path.GetDirectoryName(typeof(TestHelper).Assembly.Location) ?? "", "TestAssembly.dll");

    public static ILSpyXApplication CreateTestApplication()
    {
        var application = new ILSpyXApplication(new NullLoggerFactory());
        return application;
    }

    public static async Task<ILSpyXApplication> CreateTestApplicationWithAssembly()
    {
        var application = new ILSpyXApplication(new NullLoggerFactory());
        await application.DecompilerBackend.AddAssemblyAsync(AssemblyPath);
        return application;
    }

}
