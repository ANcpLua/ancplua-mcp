using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

/// <summary>
/// MCP tools for NuGet package operations.
/// </summary>
[McpServerToolType]
#pragma warning disable CA1812 // Internal class instantiated by MCP framework via reflection
internal sealed partial class NuGetTools(ILogger<NuGetTools> logger)
#pragma warning restore CA1812
{
    [McpServerTool]
    [Description("Search NuGet packages by query string.")]
    public async Task<PackageSearchResult[]> SearchAsync(
        [Description("Search text")] string query,
        [Description("Max results")] int take = 20,
        [Description("Custom source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        LogSearching(query);

        var repo = GetSource(source);
        var search = await repo.GetResourceAsync<PackageSearchResource>(ct).ConfigureAwait(false);

        var results = await search.SearchAsync(
            query,
            new SearchFilter(includePrerelease: true),
            skip: 0,
            take: take,
            log: NuGet.Common.NullLogger.Instance,
            cancellationToken: ct).ConfigureAwait(false);

        return [.. results.Select(r => new PackageSearchResult(
            r.Identity.Id,
            r.Identity.Version?.ToString(),
            r.Description,
            r.Authors,
            r.DownloadCount,
            r.Vulnerabilities?.Any() == true))];
    }

    [McpServerTool]
    [Description("Get detailed metadata for a package including deprecation and vulnerabilities.")]
    public async Task<PackageMetadataResult> GetMetadataAsync(
        [Description("Package ID")] string id,
        [Description("Include prerelease")] bool includePrerelease = true,
        [Description("Custom source URL (optional)")] string? source = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        LogGettingVersions(id);

        var repo = GetSource(source);
        var resource = await repo.GetResourceAsync<PackageMetadataResource>(ct).ConfigureAwait(false);
        using var cache = new SourceCacheContext();

        var items = await resource.GetMetadataAsync(
            id, includePrerelease, includeUnlisted: false, cache,
            NuGet.Common.NullLogger.Instance, ct).ConfigureAwait(false);

        var list = items.ToList();
        if (list.Count == 0)
            return new PackageMetadataResult(id, null, null, [], null, null);

        var versions = list
            .Select(m => m.Identity.Version)
            .Where(v => v is not null)
            .OrderByDescending(v => v)
            .ToList();

        var latest = list.FirstOrDefault(m => m.Identity.Version == versions.FirstOrDefault());
        var deprecation = latest is not null
            ? await latest.GetDeprecationMetadataAsync().ConfigureAwait(false)
            : null;

        return new PackageMetadataResult(
            Id: id,
            LatestStable: versions.FirstOrDefault(v => !v!.IsPrerelease)?.ToString(),
            Latest: versions.FirstOrDefault()?.ToString(),
            Versions: [.. versions.Select(v => v!.ToString())],
            Deprecation: deprecation is not null
                ? new DeprecationInfo(deprecation.Message, deprecation.Reasons?.ToArray())
                : null,
            Vulnerabilities: latest?.Vulnerabilities?.Select(v =>
                new VulnerabilityInfo(v.AdvisoryUrl?.ToString(), v.Severity switch
                {
                    0 => "Low",
                    1 => "Moderate",
                    2 => "High",
                    3 => "Critical",
                    _ => "Unknown"
                })).ToArray());
    }

    [McpServerTool]
    [Description("Get latest version for a package (shortcut for GetMetadata).")]
    public async Task<PackageMetadataResult> GetLatestAsync(
        [Description("Package ID")] string id,
        [Description("Include prerelease")] bool includePrerelease = true,
        [Description("Custom source URL (optional)")] string? source = null,
        CancellationToken ct = default)
        => await GetMetadataAsync(id, includePrerelease, source, ct).ConfigureAwait(false);

    private static SourceRepository GetSource(string? sourceUrl)
    {
        var providers = Repository.Provider.GetCoreV3();
        if (!string.IsNullOrWhiteSpace(sourceUrl))
            return new SourceRepository(new PackageSource(sourceUrl), providers);

        var settings = Settings.LoadDefaultSettings(root: null);
        var sourceProvider = new PackageSourceProvider(settings);
        var src = sourceProvider.LoadPackageSources().FirstOrDefault(s => s.IsEnabled)
                  ?? new PackageSource("https://api.nuget.org/v3/index.json");
        return new SourceRepository(src, providers);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Searching NuGet for {Query}")]
    private partial void LogSearching(string query);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting versions for package {PackageId}")]
    private partial void LogGettingVersions(string packageId);
}

// DTOs for NuGet tools - structured returns per CLAUDE.md

#pragma warning disable CA1812 // Instantiated via object initializer / collection expression

internal sealed record PackageSearchResult(
    string Id,
    string? Version,
    string? Description,
    string? Authors,
    long? Downloads,
    bool? HasVulnerabilities);

internal sealed record PackageMetadataResult(
    string Id,
    string? LatestStable,
    string? Latest,
    string[] Versions,
    DeprecationInfo? Deprecation,
    VulnerabilityInfo[]? Vulnerabilities);

internal sealed record DeprecationInfo(string? Message, string[]? Reasons);

internal sealed record VulnerabilityInfo(string? AdvisoryUrl, string? Severity);

#pragma warning restore CA1812
