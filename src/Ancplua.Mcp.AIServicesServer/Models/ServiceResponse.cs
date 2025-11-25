using System.Diagnostics.CodeAnalysis;

namespace Ancplua.Mcp.AIServicesServer.Models;

/// <summary>
/// Response from an AI service.
/// </summary>
[SuppressMessage("Performance", "CA1812", Justification = "Model for future service orchestration.")]
internal sealed record ServiceResponse
{
    /// <summary>
    /// Unique response ID
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Service that generated the response
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Response output/content
    /// </summary>
    public required string Output { get; init; }

    /// <summary>
    /// Response status (success, error, timeout)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Error message (if status is error)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Response metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Timestamp when response was generated
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
