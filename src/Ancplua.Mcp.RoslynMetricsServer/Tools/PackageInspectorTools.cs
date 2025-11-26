using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

/// <summary>
/// MCP tools for AI agents to learn unknown package APIs.
/// Downloads packages, extracts API surfaces, compares versions, decompiles to source.
/// Critical for AI agents working with late 2024/2025 packages beyond their knowledge cutoff.
/// </summary>
[McpServerToolType]
internal static class PackageInspectorTools
{

    [McpServerTool]
    [Description("Compare APIs between two package versions. Detects breaking changes, removals, additions, obsolete APIs, and sync→async migrations. Essential for understanding upgrade paths.")]
    public static async Task<ApiDiffResult> ComparePackageVersions(
        [Description("NuGet package ID (e.g., 'Newtonsoft.Json')")] string packageId,
        [Description("Source version to compare from (e.g., '12.0.0')")] string fromVersion,
        [Description("Target version to compare to (e.g., '13.0.0')")] string toVersion,
        [Description("Custom NuGet source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        var inspector = new PackageInspector(source);
        return await inspector.CompareVersionsAsync(packageId, fromVersion, toVersion, ct).ConfigureAwait(false);
    }

    [McpServerTool]
    [Description("Get a markdown report of API changes between package versions. Human-readable format showing breaking changes, deprecations, and new features.")]
    public static async Task<string> GetVersionDiffReport(
        [Description("NuGet package ID")] string packageId,
        [Description("Source version")] string fromVersion,
        [Description("Target version")] string toVersion,
        [Description("Custom NuGet source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        var inspector = new PackageInspector(source);
        var result = await inspector.CompareVersionsAsync(packageId, fromVersion, toVersion, ct).ConfigureAwait(false);

        return $"# API Changes: {packageId}\n**{fromVersion} → {toVersion}**\n\n{result.Changes.ToMarkdown()}";
    }

    [McpServerTool]
    [Description("Extract full API surface from a package. Returns all types, methods, properties with signatures. Use this to learn APIs of packages you don't know about.")]
    public static async Task<ApiSurfaceResult> ExtractPackageApi(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version")] string version,
        [Description("Include non-public members")] bool includePrivate = false,
        [Description("Custom NuGet source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        var inspector = new PackageInspector(source);
        return await inspector.ExtractApiAsync(packageId, version, includePrivate, ct).ConfigureAwait(false);
    }

    [McpServerTool]
    [Description("Decompile a package or specific type to C# source code. Use when documentation is missing or you need to understand implementation details.")]
    public static async Task<DecompileResult> DecompilePackage(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version")] string version,
        [Description("Specific type to decompile (full name). If null, decompiles entire assembly.")] string? typeName = null,
        [Description("Custom NuGet source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        var inspector = new PackageInspector(source);
        return await inspector.DecompileAsync(packageId, version, typeName, ct).ConfigureAwait(false);
    }

    [McpServerTool]
    [Description("Get a summary of obsolete/deprecated APIs in a package version. Critical for avoiding deprecated patterns.")]
    public static async Task<ObsoleteApisResult> GetObsoleteApis(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version")] string version,
        [Description("Custom NuGet source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        var inspector = new PackageInspector(source);
        var api = await inspector.ExtractApiAsync(packageId, version, false, ct).ConfigureAwait(false);

        var obsoleteTypes = api.Types
            .Where(t => t.ObsoleteMessage is not null)
            .Select(t => new ObsoleteItem(t.FullName, "type", t.ObsoleteMessage!))
            .ToList();

        var obsoleteMethods = api.Types
            .SelectMany(t => t.Methods
                .Where(m => m.ObsoleteMessage is not null)
                .Select(m => new ObsoleteItem($"{t.FullName}.{m.Name}", "method", m.ObsoleteMessage!)))
            .ToList();

        return new ObsoleteApisResult(packageId, version, [.. obsoleteTypes, .. obsoleteMethods]);
    }

    [McpServerTool]
    [Description("Quick check if a package has breaking changes between versions. Returns a simple yes/no with summary.")]
    public static async Task<BreakingChangesCheck> HasBreakingChanges(
        [Description("NuGet package ID")] string packageId,
        [Description("Source version")] string fromVersion,
        [Description("Target version")] string toVersion,
        [Description("Custom NuGet source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        var inspector = new PackageInspector(source);
        var result = await inspector.CompareVersionsAsync(packageId, fromVersion, toVersion, ct).ConfigureAwait(false);

        return new BreakingChangesCheck(
            packageId, fromVersion, toVersion,
            result.Changes.HasBreakingChanges,
            result.Changes.RemovedTypes.Count,
            result.Changes.RemovedMethods.Count,
            result.Changes.AsyncChanges.Count,
            result.Changes.HasAdditions,
            result.Changes.AddedTypes.Count,
            result.Changes.AddedMethods.Count);
    }
}

// Additional DTOs for PackageInspectorTools

#pragma warning disable CA1812

internal sealed record ObsoleteApisResult(string PackageId, string Version, ObsoleteItem[] Items);
internal sealed record ObsoleteItem(string Name, string Kind, string Message);

internal sealed record BreakingChangesCheck(
    string PackageId,
    string FromVersion,
    string ToVersion,
    bool HasBreakingChanges,
    int RemovedTypesCount,
    int RemovedMethodsCount,
    int SyncToAsyncCount,
    bool HasNewFeatures,
    int AddedTypesCount,
    int AddedMethodsCount);

#pragma warning restore CA1812
