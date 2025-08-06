namespace TestAssembly;

public interface ISomeInterface
{
    int I { get; set; }
}

public class SomeInterfaceImplementor : ISomeInterface
{
    public int I { get; set; }
}

public interface IDerivedInterface : ISomeInterface
{
    int J { get; set; }
}