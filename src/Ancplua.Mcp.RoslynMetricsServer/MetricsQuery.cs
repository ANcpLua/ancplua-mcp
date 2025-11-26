using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;

namespace Ancplua.Mcp.RoslynMetricsServer;

/// <summary>
/// Fluent query builder for code metrics.
/// </summary>
#pragma warning disable CA1812 // Class is instantiated in RoslynMetricsTools.QueryMetrics
internal sealed class MetricsQuery
#pragma warning restore CA1812
{
    private IEnumerable<CodeAnalysisMetricData> _data;

    public MetricsQuery(IEnumerable<CodeAnalysisMetricData> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;
    }

    public MetricsQuery WhereComplexityAtLeast(int min)
    {
        _data = _data.Where(m => m.CyclomaticComplexity >= min);
        return this;
    }

    public MetricsQuery WhereComplexityAtMost(int max)
    {
        _data = _data.Where(m => m.CyclomaticComplexity <= max);
        return this;
    }

    public MetricsQuery WhereMaintainabilityAtLeast(int min)
    {
        _data = _data.Where(m => m.MaintainabilityIndex >= min);
        return this;
    }

    public MetricsQuery WhereMaintainabilityAtMost(int max)
    {
        _data = _data.Where(m => m.MaintainabilityIndex <= max);
        return this;
    }

    public MetricsQuery WhereKind(SymbolKind kind)
    {
        _data = _data.Where(m => m.Symbol.Kind == kind);
        return this;
    }

    public MetricsQuery OrderByComplexity(bool descending = false)
    {
        _data = descending
            ? _data.OrderByDescending(m => m.CyclomaticComplexity)
            : _data.OrderBy(m => m.CyclomaticComplexity);
        return this;
    }

    public MetricsQuery ThenByMaintainability(bool ascending = true)
    {
        _data = _data is IOrderedEnumerable<CodeAnalysisMetricData> ordered
            ? (ascending
                ? ordered.ThenBy(m => m.MaintainabilityIndex)
                : ordered.ThenByDescending(m => m.MaintainabilityIndex))
            : (ascending
                ? _data.OrderBy(m => m.MaintainabilityIndex)
                : _data.OrderByDescending(m => m.MaintainabilityIndex));
        return this;
    }

    public MetricsQuery Take(int count)
    {
        _data = _data.Take(count);
        return this;
    }

    public IEnumerable<CodeAnalysisMetricData> ToList() => _data.ToList();

    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Symbol | Kind | Complexity | Maintainability | Lines |");
        sb.AppendLine("|--------|------|------------|-----------------|-------|");

        foreach (var m in _data)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {m.Symbol.Name} | {m.Symbol.Kind} | {m.CyclomaticComplexity} | {m.MaintainabilityIndex} | {m.SourceLines} |");
        }

        return sb.ToString();
    }
}
