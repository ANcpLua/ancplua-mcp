using Microsoft.CodeAnalysis;

namespace Ancplua.Mcp.RoslynMetricsServer;

/// <summary>
/// Extension methods for architecture analysis.
/// </summary>
internal static class ArchitectureExtensions
{
    /// <summary>
    /// Analyze project architecture.
    /// </summary>
    public static async Task<ArchitectureAnalysis> AnalyzeArchitectureAsync(
        this Project project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            return new ArchitectureAnalysis
            {
                ProjectName = project.Name,
                OutputKind = OutputKind.DynamicallyLinkedLibrary,
                IsApplication = false,
                ImpactedProjectCount = 0
            };
        }

        var outputKind = compilation.Options.OutputKind;
        var isApp = outputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication;

        // Count types
        var types = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type, cancellationToken)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        // Count dependencies (referenced projects in solution)
        var dependencyCount = project.ProjectReferences.Count();

        return new ArchitectureAnalysis
        {
            ProjectName = project.Name,
            OutputKind = outputKind,
            IsApplication = isApp,
            ImpactedProjectCount = dependencyCount,
            TypeAnalysis = new TypeAnalysis
            {
                TypeCount = types.Count,
                PublicTypeCount = types.Count(t => t.DeclaredAccessibility == Accessibility.Public),
                ClassCount = types.Count(t => t.TypeKind == TypeKind.Class),
                InterfaceCount = types.Count(t => t.TypeKind == TypeKind.Interface),
                EnumCount = types.Count(t => t.TypeKind == TypeKind.Enum),
                StructCount = types.Count(t => t.TypeKind == TypeKind.Struct)
            }
        };
    }
}

/// <summary>
/// Result of architecture analysis.
/// </summary>
internal sealed record ArchitectureAnalysis
{
    public required string ProjectName { get; init; }
    public required OutputKind OutputKind { get; init; }
    public required bool IsApplication { get; init; }
    public required int ImpactedProjectCount { get; init; }
    public TypeAnalysis? TypeAnalysis { get; init; }
}

/// <summary>
/// Type analysis results.
/// </summary>
internal sealed record TypeAnalysis
{
    public int TypeCount { get; init; }
    public int PublicTypeCount { get; init; }
    public int ClassCount { get; init; }
    public int InterfaceCount { get; init; }
    public int EnumCount { get; init; }
    public int StructCount { get; init; }
}
