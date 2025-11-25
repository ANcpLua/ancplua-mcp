using System.Text.Json.Serialization;

namespace Ancplua.Mcp.WhisperMesh.Discoveries;

/// <summary>
/// Location of a code element (file, line, symbol).
/// Used by discovery types to pinpoint findings.
/// Conforms to WhisperMesh Protocol Specification v1.0 §4.1.1.
/// </summary>
public sealed record CodeLocation
{
    /// <summary>
    /// File path (relative to repository root).
    /// Example: "src/Core/Processor.cs"
    /// </summary>
    [JsonPropertyName("file")]
    public required string File { get; init; }

    /// <summary>
    /// Line number (1-indexed).
    /// </summary>
    [JsonPropertyName("line")]
    public required int Line { get; init; }

    /// <summary>
    /// Column number (1-indexed, optional).
    /// </summary>
    [JsonPropertyName("column")]
    public int? Column { get; init; }

    /// <summary>
    /// Symbol name (method, class, field, etc.).
    /// Example: "ProcessData()"
    /// </summary>
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }
}
