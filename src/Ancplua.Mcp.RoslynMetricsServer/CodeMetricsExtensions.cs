using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;

namespace Ancplua.Mcp.RoslynMetricsServer;

/// <summary>
/// Extension methods for CodeAnalysisMetricData.
/// </summary>
internal static class CodeMetricsExtensions
{
    /// <summary>
    /// Flatten the metric data tree into a sequence of all metrics.
    /// </summary>
    public static IEnumerable<CodeAnalysisMetricData> Flatten(this CodeAnalysisMetricData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        yield return data;
        foreach (var child in data.Children)
        {
            foreach (var descendant in child.Flatten())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Count symbols of a specific kind in the metrics tree.
    /// </summary>
    public static int CountSymbolKind(this CodeAnalysisMetricData data, SymbolKind kind)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data.Flatten().Count(m => m.Symbol.Kind == kind);
    }

    /// <summary>
    /// Count named types (classes, structs, interfaces, enums, delegates).
    /// </summary>
    public static int CountNamedTypes(this CodeAnalysisMetricData data)
        => data.CountSymbolKind(SymbolKind.NamedType);

    /// <summary>
    /// Alias for CountNamedTypes for backward compatibility.
    /// </summary>
    public static int CountTypes(this CodeAnalysisMetricData data)
        => data.CountNamedTypes();

    /// <summary>
    /// Count namespaces.
    /// </summary>
    public static int CountNamespaces(this CodeAnalysisMetricData data)
        => data.CountSymbolKind(SymbolKind.Namespace);

    /// <summary>
    /// Count methods.
    /// </summary>
    public static int CountMethods(this CodeAnalysisMetricData data)
        => data.CountSymbolKind(SymbolKind.Method);

    /// <summary>
    /// Generate a summary string for the metrics.
    /// </summary>
    public static string ToSummaryString(this CodeAnalysisMetricData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Assembly: {data.Symbol.Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Cyclomatic Complexity: {data.CyclomaticComplexity}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Maintainability Index: {data.MaintainabilityIndex}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Source Lines: {data.SourceLines}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Namespaces: {data.CountNamespaces()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Types: {data.CountNamedTypes()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Methods: {data.CountMethods()}");
        return sb.ToString();
    }

    /// <summary>
    /// Generate a C# metrics report as Markdown.
    /// </summary>
    public static async Task<string> GenerateCSharpMetricsReportAsync(
        this string code,
        string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var metrics = await MetricsAnalyzer.AnalyzeCSharpAsync(code, assemblyName, cancellationToken).ConfigureAwait(false);
        var sb = new StringBuilder();

        sb.AppendLine("# Code Metrics Report");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Assembly | {metrics.Symbol.Name} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Cyclomatic Complexity | {metrics.CyclomaticComplexity} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Maintainability Index | {metrics.MaintainabilityIndex} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Source Lines | {metrics.SourceLines} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Executable Lines | {metrics.ExecutableLines} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Namespaces | {metrics.CountNamespaces()} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Types | {metrics.CountNamedTypes()} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Methods | {metrics.CountMethods()} |");
        sb.AppendLine();

        // Methods detail
        var methods = metrics.Flatten()
            .Where(m => m.Symbol.Kind == SymbolKind.Method)
            .OrderByDescending(m => m.CyclomaticComplexity)
            .Take(20);

        sb.AppendLine("## Top Methods by Complexity");
        sb.AppendLine();
        sb.AppendLine("| Method | Complexity | Maintainability | Lines |");
        sb.AppendLine("|--------|------------|-----------------|-------|");

        foreach (var method in methods)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {method.Symbol.Name} | {method.CyclomaticComplexity} | {method.MaintainabilityIndex} | {method.SourceLines} |");
        }

        return sb.ToString();
    }
}
