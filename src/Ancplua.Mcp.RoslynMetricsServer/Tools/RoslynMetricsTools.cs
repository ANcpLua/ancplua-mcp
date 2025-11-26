using System.ComponentModel;
using Microsoft.CodeAnalysis;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

/// <summary>
/// MCP tools for code metrics analysis.
/// </summary>
[McpServerToolType]
internal static class RoslynMetricsTools
{
    [McpServerTool]
    [Description("Analyze C# code and return structured metrics.")]
    public static async Task<MetricsResult> AnalyzeCSharp(
        [Description("C# source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken ct = default)
    {
        var compilation = CompilationFactory.CreateCSharp(code, assemblyName ?? "Analysis");
        var metrics = await CompilationFactory.ComputeMetricsAsync(compilation, ct).ConfigureAwait(false);
        return MetricsResult.From(metrics);
    }

    [McpServerTool]
    [Description("Analyze VB.NET code and return structured metrics.")]
    public static async Task<MetricsResult> AnalyzeVb(
        [Description("VB.NET source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken ct = default)
    {
        var compilation = CompilationFactory.CreateVisualBasic(code, assemblyName ?? "Analysis");
        var metrics = await CompilationFactory.ComputeMetricsAsync(compilation, ct).ConfigureAwait(false);
        return MetricsResult.From(metrics);
    }

    [McpServerTool]
    [Description("Generate a Markdown metrics report for C# code.")]
    public static async Task<string> GenerateCSharpReport(
        [Description("C# source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken ct = default)
    {
        var compilation = CompilationFactory.CreateCSharp(code, assemblyName ?? "Analysis");
        var metrics = await CompilationFactory.ComputeMetricsAsync(compilation, ct).ConfigureAwait(false);
        return metrics.ToMarkdownReport();
    }

    [McpServerTool]
    [Description("Query metrics from C# code with filters.")]
    public static async Task<SymbolMetrics[]> QueryMetrics(
        [Description("C# source code")] string code,
        [Description("Min cyclomatic complexity")] int? minComplexity = null,
        [Description("Max cyclomatic complexity")] int? maxComplexity = null,
        [Description("Min maintainability index")] int? minMaintainability = null,
        [Description("Symbol kind (Method|NamedType|Namespace)")] string? kind = null,
        [Description("Max results")] int take = 25,
        CancellationToken ct = default)
    {
        var compilation = CompilationFactory.CreateCSharp(code);
        var metrics = await CompilationFactory.ComputeMetricsAsync(compilation, ct).ConfigureAwait(false);

        SymbolKind? symbolKind = kind is not null && Enum.TryParse<SymbolKind>(kind, true, out var k) ? k : null;

        return [.. metrics
            .Query(minComplexity, maxComplexity, minMaintainability, symbolKind)
            .Take(take)
            .Select(SymbolMetrics.From)];
    }
}
