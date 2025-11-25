using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

[McpServerToolType]
public static class ArchitectureTools
{
    [McpServerTool]
    [Description("Analyze a synthetic project's architecture from a set of C# source files.")]
    public static async Task<object> AnalyzeProjectArchitecture(
        [Description("Array of { name, code } items")]
        IEnumerable<FileSpec> files,
        [Description("Project name")] string projectName = "AnalysisProject",
        CancellationToken cancellationToken = default)
    {
        var (solution, project) = BuildSolution(files, projectName);
        var analysis = await project.AnalyzeArchitectureAsync(cancellationToken);
        return new {
            project = project.Name,
            impactedCount = analysis.ImpactedProjectCount,
            isApp = analysis.IsApplication,
            outputKind = analysis.OutputKind.ToString(),
            types = analysis.TypeAnalysis?.TypeCount
        };
    }

    public record FileSpec(string name, string code);

    private static (Solution solution, Project project) BuildSolution(IEnumerable<FileSpec> files, string name)
    {
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var projId = ProjectId.CreateNewId();
        solution = solution.AddProject(ProjectInfo.Create(projId, VersionStamp.Create(), name, name, LanguageNames.CSharp))
                           .WithProjectCompilationOptions(projId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        foreach (var f in files)
        {
            var docId = DocumentId.CreateNewId(projId);
            solution = solution.AddDocument(DocumentInfo.Create(docId, f.name, loader: TextLoader.From(TextAndVersion.Create(SourceText.From(f.code), VersionStamp.Create()))));
        }
        return (solution, solution.GetProject(projId)!);
    }
}
