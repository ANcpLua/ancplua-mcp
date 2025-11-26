using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;

namespace Ancplua.Mcp.RoslynMetricsServer;

/// <summary>
/// Code metrics analysis result.
/// </summary>
#pragma warning disable CA1812 // Instantiated via From() factory method
internal sealed record MetricsResult(
    string Symbol,
    int Complexity,
    int Maintainability,
    long SourceLines,
    long ExecutableLines,
    int Methods,
    int Types,
    int Namespaces)
{
    /// <summary>
    /// Create from Roslyn metrics data.
    /// </summary>
    public static MetricsResult From(CodeAnalysisMetricData m) => new(
        Symbol: m.Symbol.Name,
        Complexity: m.CyclomaticComplexity,
        Maintainability: m.MaintainabilityIndex,
        SourceLines: m.SourceLines,
        ExecutableLines: m.ExecutableLines,
        Methods: m.Flatten().Count(x => x.Symbol.Kind == SymbolKind.Method),
        Types: m.Flatten().Count(x => x.Symbol.Kind == SymbolKind.NamedType),
        Namespaces: m.Flatten().Count(x => x.Symbol.Kind == SymbolKind.Namespace));
}

/// <summary>
/// Detailed symbol metrics for query results.
/// </summary>
internal sealed record SymbolMetrics(
    string Name,
    string Kind,
    int Complexity,
    int Maintainability,
    long Lines)
{
    public static SymbolMetrics From(CodeAnalysisMetricData m) => new(
        m.Symbol.Name,
        m.Symbol.Kind.ToString(),
        m.CyclomaticComplexity,
        m.MaintainabilityIndex,
        m.SourceLines);
}

/// <summary>
/// Project architecture analysis result.
/// </summary>
internal sealed record ArchitectureResult(
    string Project,
    string OutputKind,
    int Types,
    int Namespaces,
    int Methods,
    int Maintainability,
    int Complexity,
    long SourceLines)
{
    public string ToMarkdown() => new StringBuilder()
        .Append("# Project: ").AppendLine(Project)
        .AppendLine()
        .AppendLine("| Metric | Value |")
        .AppendLine("|--------|-------|")
        .Append("| Output Kind | ").Append(OutputKind).AppendLine(" |")
        .Append("| Types | ").Append(Types).AppendLine(" |")
        .Append("| Namespaces | ").Append(Namespaces).AppendLine(" |")
        .Append("| Methods | ").Append(Methods).AppendLine(" |")
        .Append("| Maintainability | ").Append(Maintainability).AppendLine(" |")
        .Append("| Complexity | ").Append(Complexity).AppendLine(" |")
        .Append("| Source Lines | ").Append(SourceLines).AppendLine(" |")
        .ToString();
}
#pragma warning restore CA1812

/// <summary>
/// Extension methods for metrics data traversal.
/// </summary>
internal static class MetricsExtensions
{
    /// <summary>
    /// Flatten the metrics tree into a sequence.
    /// </summary>
    public static IEnumerable<CodeAnalysisMetricData> Flatten(this CodeAnalysisMetricData data)
    {
        yield return data;
        foreach (var child in data.Children.SelectMany(Flatten))
            yield return child;
    }

    /// <summary>
    /// Query and filter metrics with fluent syntax.
    /// </summary>
    public static IEnumerable<CodeAnalysisMetricData> Query(
        this CodeAnalysisMetricData data,
        int? minComplexity = null,
        int? maxComplexity = null,
        int? minMaintainability = null,
        SymbolKind? kind = null)
    {
        var q = data.Flatten();

        if (minComplexity.HasValue)
            q = q.Where(m => m.CyclomaticComplexity >= minComplexity.Value);
        if (maxComplexity.HasValue)
            q = q.Where(m => m.CyclomaticComplexity <= maxComplexity.Value);
        if (minMaintainability.HasValue)
            q = q.Where(m => m.MaintainabilityIndex >= minMaintainability.Value);
        if (kind.HasValue)
            q = q.Where(m => m.Symbol.Kind == kind.Value);

        return q.OrderByDescending(m => m.CyclomaticComplexity)
                .ThenBy(m => m.MaintainabilityIndex);
    }

    /// <summary>
    /// Format metrics as markdown table.
    /// </summary>
    public static string ToMarkdownTable(this IEnumerable<CodeAnalysisMetricData> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Symbol | Kind | Complexity | Maintainability | Lines |");
        sb.AppendLine("|--------|------|------------|-----------------|-------|");

        foreach (var m in data)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| {m.Symbol.Name} | {m.Symbol.Kind} | {m.CyclomaticComplexity} | {m.MaintainabilityIndex} | {m.SourceLines} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate full markdown report.
    /// </summary>
    public static string ToMarkdownReport(this CodeAnalysisMetricData metrics)
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
        sb.AppendLine("## Top Methods by Complexity");
        sb.AppendLine();

        var topMethods = metrics.Query(kind: SymbolKind.Method).Take(20);
        sb.Append(topMethods.ToMarkdownTable());

        return sb.ToString();
    }
}
