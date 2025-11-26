using System.ComponentModel;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

/// <summary>
/// Input file specification for architecture analysis.
/// </summary>
/// <param name="Name">Source file name.</param>
/// <param name="Code">C# source code content.</param>
#pragma warning disable CA1812 // Internal class instantiated by MCP framework via JSON deserialization
internal sealed record FileSpec(string Name, string Code);
#pragma warning restore CA1812

[McpServerToolType]
internal static class ArchitectureTools
{
    [McpServerTool]
    [Description("Analyze a synthetic project's architecture from a set of C# source files.")]
    public static async Task<object> AnalyzeProjectArchitecture(
        [Description("Array of { name, code } items")]
        IEnumerable<FileSpec> files,
        [Description("Project name")] string projectName = "AnalysisProject",
        CancellationToken cancellationToken = default)
    {
        using var workspace = new AdhocWorkspace();
        var (_, project) = BuildSolution(workspace, files, projectName);

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            return new
            {
                project = projectName,
                error = "Failed to create compilation"
            };
        }

        var context = new CodeMetricsAnalysisContext(compilation, cancellationToken);
        var metrics = await CodeAnalysisMetricData.ComputeAsync(context).ConfigureAwait(false);

        var types = metrics.CountNamedTypes();
        var namespaces = metrics.CountNamespaces();
        var methods = metrics.CountMethods();

        return new
        {
            project = project.Name,
            outputKind = compilation.Options.OutputKind.ToString(),
            types,
            namespaces,
            methods,
            maintainabilityIndex = metrics.MaintainabilityIndex,
            cyclomaticComplexity = metrics.CyclomaticComplexity,
            sourceLines = metrics.SourceLines
        };
    }

    [McpServerTool]
    [Description("Get a summary of project dependencies and structure.")]
    public static async Task<string> GetProjectSummary(
        [Description("Array of { name, code } items")]
        IEnumerable<FileSpec> files,
        [Description("Project name")] string projectName = "AnalysisProject",
        CancellationToken cancellationToken = default)
    {
        using var workspace = new AdhocWorkspace();
        var (_, project) = BuildSolution(workspace, files, projectName);

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            return "Failed to create compilation";
        }

        var context = new CodeMetricsAnalysisContext(compilation, cancellationToken);
        var metrics = await CodeAnalysisMetricData.ComputeAsync(context).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.Append("# Project: ").AppendLine(projectName);
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.Append("| Output Kind | ").Append(compilation.Options.OutputKind).AppendLine(" |");
        sb.Append("| Types | ").Append(metrics.CountNamedTypes()).AppendLine(" |");
        sb.Append("| Namespaces | ").Append(metrics.CountNamespaces()).AppendLine(" |");
        sb.Append("| Methods | ").Append(metrics.CountMethods()).AppendLine(" |");
        sb.Append("| Maintainability Index | ").Append(metrics.MaintainabilityIndex).AppendLine(" |");
        sb.Append("| Cyclomatic Complexity | ").Append(metrics.CyclomaticComplexity).AppendLine(" |");
        sb.Append("| Source Lines | ").Append(metrics.SourceLines).AppendLine(" |");

        return sb.ToString();
    }

    private static (Solution Solution, Project Project) BuildSolution(AdhocWorkspace workspace, IEnumerable<FileSpec> files, string name)
    {
        var solution = workspace.CurrentSolution;
        var projId = ProjectId.CreateNewId();
        solution = solution.AddProject(ProjectInfo.Create(projId, VersionStamp.Create(), name, name, LanguageNames.CSharp))
                           .WithProjectCompilationOptions(projId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        foreach (var f in files)
        {
            var docId = DocumentId.CreateNewId(projId);
            solution = solution.AddDocument(DocumentInfo.Create(docId, f.Name, loader: TextLoader.From(TextAndVersion.Create(SourceText.From(f.Code), VersionStamp.Create()))));
        }

        return (solution, solution.GetProject(projId)!);
    }
}
