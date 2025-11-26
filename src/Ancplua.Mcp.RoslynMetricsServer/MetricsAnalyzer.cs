using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Basic.Reference.Assemblies;

namespace Ancplua.Mcp.RoslynMetricsServer;

/// <summary>
/// Static analyzer for computing code metrics from source code strings.
/// </summary>
internal static class MetricsAnalyzer
{
    /// <summary>
    /// Analyze C# source code and return code metrics.
    /// </summary>
    public static async Task<CodeAnalysisMetricData> AnalyzeCSharpAsync(
        string code,
        string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            assemblyName ?? "Analysis",
            [syntaxTree],
            NetStandard20.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await CodeAnalysisMetricData.ComputeAsync(
            compilation.Assembly,
            new CodeMetricsAnalysisContext(compilation, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Analyze VB.NET source code and return code metrics.
    /// </summary>
    public static async Task<CodeAnalysisMetricData> AnalyzeVBAsync(
        string code,
        string? assemblyName = null,
        CancellationToken cancellationToken = default)
    {
        var syntaxTree = VisualBasicSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
        var compilation = VisualBasicCompilation.Create(
            assemblyName ?? "Analysis",
            [syntaxTree],
            NetStandard20.References.All,
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await CodeAnalysisMetricData.ComputeAsync(
            compilation.Assembly,
            new CodeMetricsAnalysisContext(compilation, cancellationToken)).ConfigureAwait(false);
    }
}
