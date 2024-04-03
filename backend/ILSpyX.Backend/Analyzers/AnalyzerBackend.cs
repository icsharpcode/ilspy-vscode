using ICSharpCode.ILSpyX.Analyzers;
using ILSpy.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ILSpyX.Backend.Analyzers;

public class AnalyzerBackend
{
    public AnalyzerBackend()
    {
        Analyzers = InitAnalyzers();
    }

    public IEnumerable<IAnalyzer> Analyzers { get; }

    public IEnumerable<Node> Analyze(NodeMetadata? nodeMetadata)
    {
        return Enumerable.Empty<Node>();
    }

    private static IEnumerable<IAnalyzer> InitAnalyzers()
    {
        foreach (var analyzer in AnalyzerCollector.GetAnnotatedAnalyzers())
        {
            if (Activator.CreateInstance(analyzer.AnalyzerType) is IAnalyzer analyzerInstance)
            {
                yield return analyzerInstance;
            }
        }
    }
}