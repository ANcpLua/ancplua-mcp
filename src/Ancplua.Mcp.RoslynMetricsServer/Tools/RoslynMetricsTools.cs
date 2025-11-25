using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

[McpServerToolType]
public static class RoslynMetricsTools
{
    [McpServerTool]
    [Description("Analyze C# code and return metrics with a summary.")]
    public static async Task<object> AnalyzeCSharp(
        [Description("C# source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        var metrics = await MetricsAnalyzer.AnalyzeCSharpAsync(code, assemblyName, cancellationToken);
        return new {
            symbol = metrics.Symbol.Name,
            complexity = metrics.CyclomaticComplexity,
            maintainability = metrics.MaintainabilityIndex,
            sourceLines = metrics.SourceLines,
            methods = metrics.CountMethods(),
            types = metrics.CountTypes(),
            namespaces = metrics.CountNamespaces(),
            summary = metrics.ToSummaryString()
        };
    }

    [McpServerTool]
    [Description("Generate a Markdown metrics report for C# code.")]
    public static async Task<string> GenerateCSharpReport(
        [Description("C# source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken cancellationToken = default)
        => await code.GenerateCSharpMetricsReportAsync(assemblyName, cancellationToken);

    [McpServerTool]
    [Description("Analyze VB.NET code and return metrics.")]
    public static async Task<object> AnalyzeVb(
        [Description("VB.NET source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        var metrics = await MetricsAnalyzer.AnalyzeVBAsync(code, assemblyName, cancellationToken);
        return new { symbol = metrics.Symbol.Name, complexity = metrics.CyclomaticComplexity, maintainability = metrics.MaintainabilityIndex, sourceLines = metrics.SourceLines };
    }

    [McpServerTool]
    [Description("Query metrics from C# code with filters and return a Markdown table.")]
    public static async Task<string> QueryMetrics(
        [Description("C# source code")] string code,
        [Description("Min cyclomatic complexity")] int? minComplexity = null,
        [Description("Max cyclomatic complexity")] int? maxComplexity = null,
        [Description("Min maintainability index")] int? minMaintainability = null,
        [Description("Symbol kind to include (Method|NamedType|Namespace)")] string? kind = null,
        [Description("Max rows")] int take = 25,
        CancellationToken cancellationToken = default)
    {
        var root = await MetricsAnalyzer.AnalyzeCSharpAsync(code, null, cancellationToken);
        var query = new MetricsQuery(root.Flatten());
        if (minComplexity is int min) query = query.WhereComplexityAtLeast(min);
        if (maxComplexity is int max) query = query.WhereComplexityAtMost(max);
        if (minMaintainability is int mi) query = query.WhereMaintainabilityAtLeast(mi);
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<SymbolKind>(kind, ignoreCase: true, out var k)) query = query.WhereKind(k);
        query = query.OrderByComplexity(descending: true).ThenByMaintainability(ascending: true).Take(take);
        return query.ToMarkdown();
    }
}
