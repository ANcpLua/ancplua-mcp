using System.ComponentModel;
using Ancplua.Mcp.WhisperMesh.Models;
using Ancplua.Mcp.WhisperMesh.Services;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.WhisperMesh.Tools;

/// <summary>
/// MCP tools for WhisperMesh aggregation.
/// Exposes discovery aggregation to MCP clients (Claude Code, Rider, ancplua-claude-plugins).
/// </summary>
[McpServerToolType]
public static class WhisperAggregatorTools
{
    /// <summary>
    /// Aggregates and deduplicates WhisperMesh discoveries from multiple agents.
    /// Used by Dolphin Pod orchestrator to synthesize findings from ARCH, IMPL, and Security agents.
    /// </summary>
    /// <param name="aggregator">WhisperAggregator service (injected by MCP).</param>
    /// <param name="request">Aggregation request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated report with deduplicated discoveries, sorted by tier and severity.</returns>
    [McpServerTool]
    [Description("Aggregate and deduplicate WhisperMesh discoveries from multiple agents (Dolphin Pod workflow)")]
    public static async Task<AggregatedWhisperReportDto> AggregateDiscoveries(
        [Description("WhisperAggregator service")] WhisperAggregator aggregator,
        [Description("Aggregation request")] AggregationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Convert DTO to internal model
        var internalRequest = new AggregationRequest
        {
            Tiers = request.Tiers.Select(ParseTier).ToArray(),
            TopicPatterns = request.TopicPatterns,
            TimeWindowMinutes = request.TimeWindowMinutes,
            MinSeverity = request.MinSeverity,
            MaxDiscoveries = request.MaxDiscoveries
        };

        // Perform aggregation
        var report = await aggregator.AggregateDiscoveriesAsync(internalRequest, cancellationToken);

        // Convert to DTO
        return new AggregatedWhisperReportDto
        {
            Discoveries = report.Discoveries.Select(ConvertToDto).ToList(),
            TotalCount = report.TotalCount,
            DeduplicatedCount = report.DeduplicatedCount,
            LightningCount = report.LightningCount,
            StormCount = report.StormCount,
            CriticalCount = report.CriticalCount,
            HighCount = report.HighCount,
            MediumCount = report.MediumCount,
            LowCount = report.LowCount,
            AgentCounts = report.AgentCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            AggregatedAt = report.AggregatedAt,
            TimeWindowMinutes = report.TimeWindowMinutes
        };
    }

    /// <summary>
    /// Parses a tier string to WhisperTier enum.
    /// </summary>
    private static WhisperTier ParseTier(string tierString)
    {
        return tierString.ToLowerInvariant() switch
        {
            "lightning" => WhisperTier.Lightning,
            "storm" => WhisperTier.Storm,
            _ => throw new ArgumentException($"Invalid tier: {tierString}. Must be 'lightning' or 'storm'.")
        };
    }

    /// <summary>
    /// Converts WhisperMessage to DTO for JSON serialization.
    /// </summary>
    private static WhisperMessageDto ConvertToDto(WhisperMessage message)
    {
        return new WhisperMessageDto
        {
            MessageId = message.MessageId,
            Agent = message.Agent,
            Tier = message.Tier.ToString().ToLowerInvariant(),
            Topic = message.Topic,
            Severity = message.Severity,
            Message = message.Message,
            Discovery = message.Discovery,
            Metadata = message.Metadata,
            Timestamp = message.Timestamp,
            CorrelationId = message.CorrelationId,
            ExpiresAt = message.ExpiresAt
        };
    }
}

/// <summary>
/// DTO for aggregation request (MCP tool input).
/// </summary>
public sealed record AggregationRequestDto
{
    /// <summary>
    /// Tiers to aggregate from (lightning, storm, or both).
    /// Example: ["lightning", "storm"]
    /// </summary>
    [Description("Tiers to aggregate from (lightning, storm, or both)")]
    public required string[] Tiers { get; init; }

    /// <summary>
    /// Topic patterns to subscribe to (supports NATS wildcards: * and >).
    /// Example: ["security.*", "code-quality", "architecture"]
    /// </summary>
    [Description("Topic patterns to subscribe to (supports NATS wildcards)")]
    public required string[] TopicPatterns { get; init; }

    /// <summary>
    /// Time window in minutes to collect discoveries.
    /// Default: 5 minutes.
    /// </summary>
    [Description("Time window in minutes (default: 5)")]
    public int TimeWindowMinutes { get; init; } = 5;

    /// <summary>
    /// Minimum severity threshold (0.0-1.0).
    /// Default: 0.0 (include all).
    /// </summary>
    [Description("Minimum severity threshold 0.0-1.0 (default: 0.0)")]
    public double MinSeverity { get; init; } = 0.0;

    /// <summary>
    /// Maximum number of discoveries to collect.
    /// Default: 1000.
    /// </summary>
    [Description("Maximum discoveries to collect (default: 1000)")]
    public int MaxDiscoveries { get; init; } = 1000;
}

/// <summary>
/// DTO for aggregated report (MCP tool output).
/// </summary>
public sealed record AggregatedWhisperReportDto
{
    /// <summary>
    /// Deduplicated and sorted discoveries.
    /// </summary>
    public required List<WhisperMessageDto> Discoveries { get; init; }

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
    public required Dictionary<string, int> AgentCounts { get; init; }

    /// <summary>
    /// Timestamp when aggregation completed.
    /// </summary>
    public required DateTimeOffset AggregatedAt { get; init; }

    /// <summary>
    /// Time window used for aggregation (in minutes).
    /// </summary>
    public required int TimeWindowMinutes { get; init; }
}

/// <summary>
/// DTO for WhisperMessage (MCP tool output).
/// </summary>
public sealed record WhisperMessageDto
{
    public required string MessageId { get; init; }
    public required string Agent { get; init; }
    public required string Tier { get; init; }
    public required string Topic { get; init; }
    public required double Severity { get; init; }
    public string? Message { get; init; }
    public System.Text.Json.JsonElement? Discovery { get; init; }
    public WhisperMetadata? Metadata { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
