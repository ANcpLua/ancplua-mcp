using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

[McpServerToolType]
#pragma warning disable CA1812 // Internal class instantiated by MCP framework via reflection
internal sealed partial class NuGetTools(ILogger<NuGetTools> logger)
#pragma warning restore CA1812
{
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

    [McpServerTool]
    [Description("Search NuGet packages by query string.")]
    public async Task<object> SearchAsync(
        [Description("Search text")] string query,
        [Description("Max results")] int take = 20,
        [Description("Custom source URL (optional)")] string? source = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        LogSearching(query);

        var repo = GetSource(source);
        var search = await repo.GetResourceAsync<PackageSearchResource>(cancellationToken).ConfigureAwait(false);

        var results = await search.SearchAsync(
            query,
            new SearchFilter(includePrerelease: true),
            skip: 0,
            take: take,
            log: NuGet.Common.NullLogger.Instance,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return results.Select(r => new
        {
            id = r.Identity.Id,
            version = r.Identity.Version?.ToString(),
            description = r.Description,
            authors = r.Authors,
            totalDownloads = r.DownloadCount
        }).ToArray();
    }

    [McpServerTool]
    [Description("Get all versions for a package id.")]
    public async Task<object> GetVersionsAsync(
        [Description("Package ID")] string id,
        [Description("Include prerelease")] bool includePrerelease = true,
        [Description("Custom source URL (optional)")] string? source = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        LogGettingVersions(id);

        var repo = GetSource(source);
        var metadata = await repo.GetResourceAsync<PackageMetadataResource>(cancellationToken).ConfigureAwait(false);
        using var cache = new SourceCacheContext();
        var items = await metadata.GetMetadataAsync(
            id,
            includePrerelease,
            includeUnlisted: false,
            cache,
            NuGet.Common.NullLogger.Instance,
            cancellationToken).ConfigureAwait(false);

        var versions = items.Select(m => m.Identity.Version)
                            .OfType<NuGetVersion>()
                            .OrderByDescending(v => v)
                            .ToList();

        return new
        {
            id,
            latestStable = versions.FirstOrDefault(v => !v.IsPrerelease)?.ToString(),
            latest = versions.FirstOrDefault()?.ToString(),
            versions = versions.Select(v => v.ToString()).ToArray()
        };
    }

    [McpServerTool]
    [Description("Get latest and latest stable version for a package id.")]
    public async Task<object> GetLatestAsync(
        [Description("Package ID")] string id,
        [Description("Include prerelease")] bool includePrerelease = true,
        [Description("Custom source URL (optional)")] string? source = null,
        CancellationToken cancellationToken = default)
        => await GetVersionsAsync(id, includePrerelease, source, cancellationToken).ConfigureAwait(false);

    [LoggerMessage(Level = LogLevel.Information, Message = "Searching NuGet for {Query}")]
    private partial void LogSearching(string query);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting versions for package {PackageId}")]
    private partial void LogGettingVersions(string packageId);
}
