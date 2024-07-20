using ICSharpCode.ILSpyX.Analyzers;
using System;
using System.Collections.Generic;
using System.Reflection;

public class AnalyzerCollector
{
    public static IEnumerable<(ExportAnalyzerAttribute AttributeData, Type AnalyzerType)> GetAnnotatedAnalyzers()
    {
        foreach (var type in typeof(ExportAnalyzerAttribute).Assembly.GetTypes())
        {
            if (type.GetCustomAttribute(typeof(ExportAnalyzerAttribute), false) is ExportAnalyzerAttribute exportAnalyzerAttribute)
            {
                yield return (exportAnalyzerAttribute, type);
            }
        }
    }
}