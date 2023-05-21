using ILSpy.Backend.Decompiler;
using Microsoft.Extensions.Logging.Abstractions;

namespace ILSpy.Backend.Tests;

public class ListAssemblyReferencesTests
{
    [Fact]
    public void ListAssemblyReferencesFromTestAssembly()
    {
        var d = new DecompilerBackend(new NullLoggerFactory(), new ILSpyBackendSettings());
        string path = Path.Combine(Path.GetDirectoryName(typeof(ListAssemblyReferencesTests).Assembly.Location) ?? "", "TestAssembly.dll");
        d.AddAssembly(path);
        string[] references = d.ListAssemblyReferences(path).ToArray();
        Assert.Single(references);
        Assert.StartsWith("System.Runtime, Version=", references[0]);
    }
}
