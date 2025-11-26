using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Ancplua.Mcp.RoslynMetricsServer;

/// <summary>
/// Single source of truth for creating compilations from source code.
/// </summary>
internal static class CompilationFactory
{
    /// <summary>
    /// Create a C# compilation from source code.
    /// </summary>
    public static CSharpCompilation CreateCSharp(string code, string assemblyName = "Analysis")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(code)],
            NetStandard20.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Create a VB.NET compilation from source code.
    /// </summary>
    public static VisualBasicCompilation CreateVisualBasic(string code, string assemblyName = "Analysis")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return VisualBasicCompilation.Create(
            assemblyName,
            [VisualBasicSyntaxTree.ParseText(code)],
            NetStandard20.References.All,
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Create a multi-file project compilation for architecture analysis.
    /// </summary>
    public static CSharpCompilation CreateProject(IEnumerable<SourceFile> files, string projectName = "Project")
    {
        ArgumentNullException.ThrowIfNull(files);
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(
            SourceText.From(f.Code),
            path: f.Name));

        return CSharpCompilation.Create(
            projectName,
            trees,
            NetStandard20.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Compute metrics for a compilation.
    /// </summary>
    public static async Task<CodeAnalysisMetricData> ComputeMetricsAsync(
        Compilation compilation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        var context = new CodeMetricsAnalysisContext(compilation, ct);
        return await CodeAnalysisMetricData.ComputeAsync(context).ConfigureAwait(false);
    }
}

/// <summary>
/// Source file for multi-file compilation.
/// </summary>
/// <param name="Name">File name (e.g., "Program.cs").</param>
/// <param name="Code">Source code content.</param>
#pragma warning disable CA1812 // Instantiated via MCP JSON deserialization
internal sealed record SourceFile(string Name, string Code);
#pragma warning restore CA1812
