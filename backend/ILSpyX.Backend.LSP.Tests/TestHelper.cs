using ILSpyX.Backend.Application;
using Microsoft.Extensions.Logging.Abstractions;

namespace ILSpyX.Backend.LSP.Tests;

public class TestHelper
{
    public static string AssemblyPath => Path.Combine(Path.GetDirectoryName(typeof(TestHelper).Assembly.Location) ?? "",
        "TestAssembly.dll");

    public static ILSpyXApplication CreateTestApplication()
    {
        var application = new ILSpyXApplication(new NullLoggerFactory());
        return application;
    }
}