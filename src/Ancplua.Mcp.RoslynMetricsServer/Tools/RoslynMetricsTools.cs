using System.ComponentModel;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

[McpServerToolType]
internal static class RoslynMetricsTools
{
    [McpServerTool]
    [Description("Analyze C# code and return metrics with a summary.")]
    public static async Task<object> AnalyzeCSharp(
        [Description("C# source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        var compilation = CreateCSharpCompilation(code, assemblyName ?? "AnalysisAssembly");
        var context = new CodeMetricsAnalysisContext(compilation, cancellationToken);
        var metrics = await CodeAnalysisMetricData.ComputeAsync(context).ConfigureAwait(false);

        return new
        {
            symbol = metrics.Symbol.Name,
            complexity = metrics.CyclomaticComplexity,
            maintainability = metrics.MaintainabilityIndex,
            sourceLines = metrics.SourceLines,
            executableLines = metrics.ExecutableLines,
            methods = metrics.CountMethods(),
            types = metrics.CountNamedTypes(),
            namespaces = metrics.CountNamespaces(),
            summary = metrics.ToString()
        };
    }

    [McpServerTool]
    [Description("Generate a Markdown metrics report for C# code.")]
    public static async Task<string> GenerateCSharpReport(
        [Description("C# source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        var compilation = CreateCSharpCompilation(code, assemblyName ?? "AnalysisAssembly");
        var context = new CodeMetricsAnalysisContext(compilation, cancellationToken);
        var metrics = await CodeAnalysisMetricData.ComputeAsync(context).ConfigureAwait(false);

        return GenerateMarkdownReport(metrics);
    }

    [McpServerTool]
    [Description("Analyze VB.NET code and return metrics.")]
    public static async Task<object> AnalyzeVb(
        [Description("VB.NET source code")] string code,
        [Description("Assembly name (optional)")] string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        var compilation = CreateVBCompilation(code, assemblyName ?? "AnalysisAssembly");
        var context = new CodeMetricsAnalysisContext(compilation, cancellationToken);
        var metrics = await CodeAnalysisMetricData.ComputeAsync(context).ConfigureAwait(false);

        return new
        {
            symbol = metrics.Symbol.Name,
            complexity = metrics.CyclomaticComplexity,
            maintainability = metrics.MaintainabilityIndex,
            sourceLines = metrics.SourceLines
        };
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
        var compilation = CreateCSharpCompilation(code, "AnalysisAssembly");
        var context = new CodeMetricsAnalysisContext(compilation, cancellationToken);
        var metrics = await CodeAnalysisMetricData.ComputeAsync(context).ConfigureAwait(false);

        var flattened = metrics.Flatten();

        // Apply filters
        var filtered = flattened;
        if (minComplexity.HasValue)
            filtered = filtered.Where(m => m.CyclomaticComplexity >= minComplexity.Value);
        if (maxComplexity.HasValue)
            filtered = filtered.Where(m => m.CyclomaticComplexity <= maxComplexity.Value);
        if (minMaintainability.HasValue)
            filtered = filtered.Where(m => m.MaintainabilityIndex >= minMaintainability.Value);
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<SymbolKind>(kind, ignoreCase: true, out var k))
            filtered = filtered.Where(m => m.Symbol.Kind == k);

        var results = filtered
            .OrderByDescending(m => m.CyclomaticComplexity)
            .ThenBy(m => m.MaintainabilityIndex)
            .Take(take)
            .ToArray();

        return GenerateMarkdownTable(results);
    }

    private static CSharpCompilation CreateCSharpCompilation(string code, string assemblyName)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return CSharpCompilation.Create(
            assemblyName,
            [tree],
            Basic.Reference.Assemblies.NetStandard20.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static VisualBasicCompilation CreateVBCompilation(string code, string assemblyName)
    {
        var tree = VisualBasicSyntaxTree.ParseText(code);
        return VisualBasicCompilation.Create(
            assemblyName,
            [tree],
            Basic.Reference.Assemblies.NetStandard20.References.All,
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static string GenerateMarkdownReport(CodeAnalysisMetricData metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Code Metrics Report");
        sb.AppendLine();
        sb.Append("**Assembly:** ").AppendLine(metrics.Symbol.Name);
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.Append("| Maintainability Index | ").Append(metrics.MaintainabilityIndex).AppendLine(" |");
        sb.Append("| Cyclomatic Complexity | ").Append(metrics.CyclomaticComplexity).AppendLine(" |");
        sb.Append("| Source Lines | ").Append(metrics.SourceLines).AppendLine(" |");
        sb.Append("| Executable Lines | ").Append(metrics.ExecutableLines).AppendLine(" |");
        sb.AppendLine();
        sb.AppendLine("## Details");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(metrics.ToString());
        sb.AppendLine("```");

        return sb.ToString();
    }

    private static string GenerateMarkdownTable(IReadOnlyList<CodeAnalysisMetricData> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Symbol | Kind | Complexity | Maintainability | Lines |");
        sb.AppendLine("|--------|------|------------|-----------------|-------|");

        foreach (var m in results)
        {
            sb.Append("| ").Append(m.Symbol.Name)
              .Append(" | ").Append(m.Symbol.Kind)
              .Append(" | ").Append(m.CyclomaticComplexity)
              .Append(" | ").Append(m.MaintainabilityIndex)
              .Append(" | ").Append(m.SourceLines).AppendLine(" |");
        }

        return sb.ToString();
    }
}
