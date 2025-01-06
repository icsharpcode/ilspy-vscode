using ICSharpCode.ILSpyX.Analyzers;
using ILSpyX.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ILSpyX.Backend.Analyzers;

public class AnalyzerInstance(IAnalyzer instance, string header, int order)
{
    public IAnalyzer Instance { get; } = instance;
    public string Header { get; } = header;
    public int Order { get; } = order;

    public string NodeSubType => Instance.GetType().Name;
}

public class AnalyzerBackend
{
    public AnalyzerBackend()
    {
        Analyzers = InitAnalyzers().OrderBy(analyzer => analyzer.Order);
    }

    public IEnumerable<AnalyzerInstance> Analyzers { get; }

    public IEnumerable<Node> Analyze(NodeMetadata? nodeMetadata)
    {
        return [];
    }

    public AnalyzerInstance? GetAnalyzerForNode(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is not null)
        {
            return Analyzers.FirstOrDefault(analyzer => analyzer.NodeSubType == nodeMetadata.SubType);
        }

        return null;
    }

    private static IEnumerable<AnalyzerInstance> InitAnalyzers()
    {
        foreach (var analyzer in AnalyzerCollector.GetAnnotatedAnalyzers())
        {
            if (Activator.CreateInstance(analyzer.AnalyzerType) is IAnalyzer analyzerInstance)
            {
                yield return new AnalyzerInstance(analyzerInstance, analyzer.AttributeData.Header, analyzer.AttributeData.Order);
            }
        }
    }
}