using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Ancplua.Mcp.RoslynMetricsServer.Tools;

[McpServerToolType]
public class NuGetTools(ILogger<NuGetTools> logger)
{
    private async Task<SourceRepository> GetSourceAsync(string? sourceUrl)
    {
        var providers = Repository.Provider.GetCoreV3();
        if (!string.IsNullOrWhiteSpace(sourceUrl))
            return Repository.CreateSource(new PackageSource(sourceUrl), providers);

        var settings = Settings.LoadDefaultSettings(root: null);
        var sourceProvider = new PackageSourceProvider(settings);
        var src = sourceProvider.LoadPackageSources().FirstOrDefault(s => s.IsEnabled)
                  ?? new PackageSource("https://api.nuget.org/v3/index.json");
        return Repository.CreateSource(src, providers);
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

        logger.LogInformation("Searching NuGet for {Query}", query);

        var repo = await GetSourceAsync(source);
        var search = await repo.GetResourceAsync<PackageSearchResource>(cancellationToken);

        var results = await search.SearchAsync(query, new SearchFilter(includePrerelease: true), skip: 0, take: take, log: NullLogger.Instance, cancellationToken);

        return results.Select(r => new {
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

        logger.LogInformation("Getting versions for package {PackageId}", id);

        var repo = await GetSourceAsync(source);
        var metadata = await repo.GetResourceAsync<PackageMetadataResource>(cancellationToken);
        var items = await metadata.GetMetadataAsync(id, includePrerelease, includeUnlisted: false, NullLogger.Instance, cancellationToken);

        var versions = items.Select(m => m.Identity.Version)
                            .OfType<NuGetVersion>()
                            .OrderByDescending(v => v)
                            .ToList();

        return new {
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
        => await GetVersionsAsync(id, includePrerelease, source, cancellationToken);
}
