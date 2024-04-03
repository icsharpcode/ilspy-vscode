using ICSharpCode.ILSpyX.Analyzers;
using ICSharpCode.ILSpyX.Analyzers.Builtin;
using ILSpy.Backend.Model;

namespace ILSpyX.Backend.Analyzer;

public class AnalyzerBackend
{
    private readonly List<IAnalyzer> analyzers;

    public AnalyzerBackend()
    {
        analyzers = new List<IAnalyzer> {
            new AttributeAppliedToAnalyzer(),
            new EventImplementedByAnalyzer(),
            new EventOverriddenByAnalyzer(),
            new AssignedByFieldAccessAnalyzer(),
            new ReadByFieldAccessAnalyzer(),
            new MemberImplementsInterfaceAnalyzer(),
            new MethodImplementedByAnalyzer(),
            new MethodOverriddenByAnalyzer(),
            new MethodUsedByAnalyzer(),
            new MethodUsesAnalyzer(),
            new MethodVirtualUsedByAnalyzer(),
            new PropertyImplementedByAnalyzer(),
            new PropertyOverriddenByAnalyzer(),
            new TypeExposedByAnalyzer(),
            new TypeExtensionMethodsAnalyzer(),
            new TypeInstantiatedByAnalyzer(),
            new TypeUsedByAnalyzer()
        };
    }

    public IEnumerable<IAnalyzer> Analyzers => analyzers;

    public IEnumerable<Node> Analyze(NodeMetadata? nodeMetadata)
    {
        return Enumerable.Empty<Node>();
    }
}