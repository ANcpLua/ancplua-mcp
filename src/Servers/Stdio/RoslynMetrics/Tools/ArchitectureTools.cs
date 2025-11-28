using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.Servers.Stdio.RoslynMetrics.Tools;

/// <summary>
/// MCP tools for project architecture analysis.
/// </summary>
[McpServerToolType]
internal static class ArchitectureTools
{
    [McpServerTool]
    [Description("Analyze a project's architecture from C# source files.")]
    public static async Task<ArchitectureResult> AnalyzeProjectArchitecture(
        [Description("Array of { name, code } source files")]
        IEnumerable<SourceFile> files,
        [Description("Project name")] string projectName = "Project",
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var compilation = CompilationFactory.CreateProject(files, projectName);
        var metrics = await CompilationFactory.ComputeMetricsAsync(compilation, ct).ConfigureAwait(false);

        return new ArchitectureResult(
            Project: projectName,
            OutputKind: compilation.Options.OutputKind.ToString(),
            Types: metrics.Flatten().Count(m => m.Symbol.Kind == Microsoft.CodeAnalysis.SymbolKind.NamedType),
            Namespaces: metrics.Flatten().Count(m => m.Symbol.Kind == Microsoft.CodeAnalysis.SymbolKind.Namespace),
            Methods: metrics.Flatten().Count(m => m.Symbol.Kind == Microsoft.CodeAnalysis.SymbolKind.Method),
            Maintainability: metrics.MaintainabilityIndex,
            Complexity: metrics.CyclomaticComplexity,
            SourceLines: metrics.SourceLines);
    }

    [McpServerTool]
    [Description("Get a markdown summary of project structure.")]
    public static async Task<string> GetProjectSummary(
        [Description("Array of { name, code } source files")]
        IEnumerable<SourceFile> files,
        [Description("Project name")] string projectName = "Project",
        CancellationToken ct = default)
    {
        var result = await AnalyzeProjectArchitecture(files, projectName, ct).ConfigureAwait(false);
        return result.ToMarkdown();
    }
}
