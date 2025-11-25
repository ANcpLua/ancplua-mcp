using System.Text.Json;
using Ancplua.Mcp.WhisperMesh.Client;
using Ancplua.Mcp.WhisperMesh.Discoveries;
using Ancplua.Mcp.WhisperMesh.Models;
using Microsoft.Extensions.Logging;

namespace Ancplua.Mcp.WhisperMesh.Services;

/// <summary>
/// Aggregates and deduplicates WhisperMesh discoveries from multiple agents.
/// Used by Dolphin Pod orchestrator to synthesize findings from ARCH, IMPL, and Security agents.
/// </summary>
public sealed class WhisperAggregator
{
    private readonly IWhisperMeshClient _client;
    private readonly ILogger<WhisperAggregator> _logger;

    public WhisperAggregator(
        IWhisperMeshClient client,
        ILogger<WhisperAggregator> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates discoveries from multiple agents/sources.
    /// </summary>
    /// <param name="request">Aggregation request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated report with deduplicated discoveries.</returns>
    public async Task<AggregatedWhisperReport> AggregateDiscoveriesAsync(
        AggregationRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var discoveries = new List<WhisperMessage>();
        var agentCounts = new Dictionary<string, int>();

        _logger.LogInformation(
            "Starting aggregation: tiers={Tiers}, topics={Topics}, window={WindowMinutes}min, minSeverity={MinSeverity}",
            string.Join(",", request.Tiers),
            string.Join(",", request.TopicPatterns),
            request.TimeWindowMinutes,
            request.MinSeverity);

        try
        {
            // Subscribe to all requested tier/topic combinations
            foreach (var tier in request.Tiers)
            {
                foreach (var topicPattern in request.TopicPatterns)
                {
                    await foreach (var message in _client.SubscribeAsync(tier, topicPattern, cancellationToken))
                    {
                        // Apply time window filter
                        var age = DateTimeOffset.UtcNow - message.Timestamp;
                        if (age.TotalMinutes > request.TimeWindowMinutes)
                        {
                            continue;
                        }

                        // Apply severity filter
                        if (message.Severity < request.MinSeverity)
                        {
                            continue;
                        }

                        discoveries.Add(message);

                        // Track per-agent counts
                        if (!agentCounts.ContainsKey(message.Agent))
                        {
                            agentCounts[message.Agent] = 0;
                        }
                        agentCounts[message.Agent]++;

                        // Stop if we've collected enough or exceeded time window
                        if (discoveries.Count >= request.MaxDiscoveries)
                        {
                            _logger.LogDebug("Reached max discoveries limit: {MaxDiscoveries}", request.MaxDiscoveries);
                            break;
                        }

                        var elapsed = DateTimeOffset.UtcNow - startTime;
                        if (elapsed.TotalMinutes >= request.TimeWindowMinutes)
                        {
                            _logger.LogDebug("Reached time window limit: {TimeWindowMinutes}min", request.TimeWindowMinutes);
                            break;
                        }
                    }
                }
            }

            // Deduplicate discoveries
            var deduplicated = DeduplicateDiscoveries(discoveries);

            // Sort by tier (Lightning first) then by severity (descending)
            var sorted = deduplicated
                .OrderBy(d => d.Tier == WhisperTier.Lightning ? 0 : 1)
                .ThenByDescending(d => d.Severity)
                .ToList();

            // Count by tier
            var lightningCount = sorted.Count(d => d.Tier == WhisperTier.Lightning);
            var stormCount = sorted.Count(d => d.Tier == WhisperTier.Storm);

            // Count critical discoveries (severity >= 0.8)
            var criticalCount = sorted.Count(d => d.Severity >= 0.8);
            var highCount = sorted.Count(d => d.Severity >= 0.6 && d.Severity < 0.8);
            var mediumCount = sorted.Count(d => d.Severity >= 0.4 && d.Severity < 0.6);
            var lowCount = sorted.Count(d => d.Severity < 0.4);

            _logger.LogInformation(
                "Aggregation complete: total={Total}, deduplicated={Deduplicated}, lightning={Lightning}, storm={Storm}, critical={Critical}",
                discoveries.Count,
                sorted.Count,
                lightningCount,
                stormCount,
                criticalCount);

            return new AggregatedWhisperReport
            {
                Discoveries = sorted,
                TotalCount = discoveries.Count,
                DeduplicatedCount = sorted.Count,
                LightningCount = lightningCount,
                StormCount = stormCount,
                CriticalCount = criticalCount,
                HighCount = highCount,
                MediumCount = mediumCount,
                LowCount = lowCount,
                AgentCounts = agentCounts,
                AggregatedAt = DateTimeOffset.UtcNow,
                TimeWindowMinutes = request.TimeWindowMinutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aggregation failed");
            throw;
        }
    }

    /// <summary>
    /// Deduplicates discoveries based on CodeLocation and category.
    /// If multiple discoveries have the same location and category, keeps the one with highest severity.
    /// </summary>
    private List<WhisperMessage> DeduplicateDiscoveries(List<WhisperMessage> discoveries)
    {
        var deduplicationKeys = new Dictionary<string, WhisperMessage>();

        foreach (var discovery in discoveries)
        {
            var key = BuildDeduplicationKey(discovery);
            if (key == null)
            {
                // Cannot deduplicate without location, keep it
                deduplicationKeys[$"unique-{discovery.MessageId}"] = discovery;
                continue;
            }

            // If this key already exists, keep the one with higher severity
            if (deduplicationKeys.TryGetValue(key, out var existing))
            {
                if (discovery.Severity > existing.Severity)
                {
                    deduplicationKeys[key] = discovery;
                    _logger.LogDebug(
                        "Replaced duplicate discovery at {Key}: old severity={OldSeverity}, new severity={NewSeverity}",
                        key,
                        existing.Severity,
                        discovery.Severity);
                }
            }
            else
            {
                deduplicationKeys[key] = discovery;
            }
        }

        return deduplicationKeys.Values.ToList();
    }

    /// <summary>
    /// Builds a deduplication key from a discovery.
    /// Format: {file}:{line}:{category}
    /// Returns null if discovery doesn't have sufficient location info.
    /// </summary>
    private string? BuildDeduplicationKey(WhisperMessage message)
    {
        try
        {
            // Try to extract CodeLocation from discovery
            if (message.Discovery?.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var location = ExtractCodeLocation(message.Discovery.Value);
            if (location == null)
            {
                return null;
            }

            // Extract category from discovery type
            var category = ExtractCategory(message.Discovery.Value);

            return $"{location.File}:{location.Line}:{category}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build deduplication key for message: {MessageId}", message.MessageId);
            return null;
        }
    }

    /// <summary>
    /// Extracts CodeLocation from a discovery JsonElement.
    /// </summary>
    private CodeLocation? ExtractCodeLocation(JsonElement discovery)
    {
        if (!discovery.TryGetProperty("location", out var locationElement))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CodeLocation>(locationElement.GetRawText());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts category from discovery JsonElement.
    /// For ArchitectureViolation: uses "rule"
    /// For ImplementationIssue: uses "category"
    /// </summary>
    private string ExtractCategory(JsonElement discovery)
    {
        if (!discovery.TryGetProperty("type", out var typeElement))
        {
            return "unknown";
        }

        var type = typeElement.GetString();

        return type switch
        {
            "ArchitectureViolation" when discovery.TryGetProperty("rule", out var rule) => rule.GetString() ?? "unknown",
            "ImplementationIssue" when discovery.TryGetProperty("category", out var cat) => cat.GetString() ?? "unknown",
            _ => type ?? "unknown"
        };
    }
}

/// <summary>
/// Request parameters for discovery aggregation.
/// </summary>
public sealed record AggregationRequest
{
    /// <summary>
    /// Tiers to aggregate from (Lightning, Storm, or both).
    /// </summary>
    public required WhisperTier[] Tiers { get; init; }

    /// <summary>
    /// Topic patterns to subscribe to (supports NATS wildcards: * and >).
    /// Example: ["security.*", "code-quality"]
    /// </summary>
    public required string[] TopicPatterns { get; init; }

    /// <summary>
    /// Time window in minutes to collect discoveries.
    /// Only discoveries within this window will be included.
    /// Default: 5 minutes.
    /// </summary>
    public int TimeWindowMinutes { get; init; } = 5;

    /// <summary>
    /// Minimum severity threshold (0.0-1.0).
    /// Discoveries below this threshold are filtered out.
    /// Default: 0.0 (include all).
    /// </summary>
    public double MinSeverity { get; init; } = 0.0;

    /// <summary>
    /// Maximum number of discoveries to collect.
    /// Default: 1000.
    /// </summary>
    public int MaxDiscoveries { get; init; } = 1000;
}

/// <summary>
/// Aggregated report of WhisperMesh discoveries.
/// </summary>
public sealed record AggregatedWhisperReport
{
    /// <summary>
    /// Deduplicated and sorted discoveries.
    /// Sorted by tier (Lightning first), then severity (descending).
    /// </summary>
    public required IReadOnlyList<WhisperMessage> Discoveries { get; init; }

    /// <summary>
    /// Total number of discoveries collected (before deduplication).
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Number of discoveries after deduplication.
    /// </summary>
    public required int DeduplicatedCount { get; init; }

    /// <summary>
    /// Number of Lightning tier discoveries.
    /// </summary>
    public required int LightningCount { get; init; }

    /// <summary>
    /// Number of Storm tier discoveries.
    /// </summary>
    public required int StormCount { get; init; }

    /// <summary>
    /// Number of critical discoveries (severity >= 0.8).
    /// </summary>
    public required int CriticalCount { get; init; }

    /// <summary>
    /// Number of high severity discoveries (0.6 <= severity < 0.8).
    /// </summary>
    public required int HighCount { get; init; }

    /// <summary>
    /// Number of medium severity discoveries (0.4 <= severity < 0.6).
    /// </summary>
    public required int MediumCount { get; init; }

    /// <summary>
    /// Number of low severity discoveries (severity < 0.4).
    /// </summary>
    public required int LowCount { get; init; }

    /// <summary>
    /// Per-agent discovery counts.
    /// </summary>
    public required IReadOnlyDictionary<string, int> AgentCounts { get; init; }

    /// <summary>
    /// Timestamp when aggregation completed.
    /// </summary>
    public required DateTimeOffset AggregatedAt { get; init; }

    /// <summary>
    /// Time window used for aggregation (in minutes).
    /// </summary>
    public required int TimeWindowMinutes { get; init; }
}
