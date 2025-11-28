using System.Text.Json.Serialization;

namespace Ancplua.Mcp.Libraries.WhisperMesh.Models;

/// <summary>
/// Context metadata for whisper traceability.
/// Conforms to WhisperMesh Protocol Specification v1.0 §2.1.
/// </summary>
public sealed record WhisperMetadata
{
    /// <summary>
    /// Project name (e.g., "ancplua-mcp").
    /// </summary>
    [JsonPropertyName("project")]
    public string? Project { get; init; }

    /// <summary>
    /// Repository URL (e.g., "https://github.com/ANcpLua/ancplua-mcp").
    /// </summary>
    [JsonPropertyName("repository")]
    public string? Repository { get; init; }

    /// <summary>
    /// Git commit SHA (40-char hex).
    /// </summary>
    [JsonPropertyName("commit")]
    public string? Commit { get; init; }

    /// <summary>
    /// Git branch name.
    /// </summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; init; }

    /// <summary>
    /// Tool version that emitted the whisper.
    /// </summary>
    [JsonPropertyName("toolVersion")]
    public string? ToolVersion { get; init; }

    /// <summary>
    /// Programming language (e.g., "csharp", "python").
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    /// <summary>
    /// Framework/runtime (e.g., "net10.0", "python3.12").
    /// </summary>
    [JsonPropertyName("framework")]
    public string? Framework { get; init; }

    /// <summary>
    /// WhisperMesh protocol schema version (e.g., "1.0.0").
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// OpenTelemetry trace ID for distributed tracing.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// OpenTelemetry span ID for distributed tracing.
    /// </summary>
    [JsonPropertyName("spanId")]
    public string? SpanId { get; init; }
}
