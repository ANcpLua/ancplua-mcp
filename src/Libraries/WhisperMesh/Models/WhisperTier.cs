using System.Text.Json.Serialization;

namespace Ancplua.Mcp.Libraries.WhisperMesh.Models;

/// <summary>
/// WhisperMesh tier: Lightning (critical, durable) or Storm (ambient, ephemeral).
/// Conforms to WhisperMesh Protocol Specification v1.0 §2.3.1.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WhisperTier>))]
public enum WhisperTier
{
    /// <summary>
    /// Critical discoveries (CVEs, build failures, breaking changes).
    /// JetStream: durable, 24h TTL, file-backed.
    /// NATS subject: ancplua.lightning.*
    /// </summary>
    Lightning,

    /// <summary>
    /// Ambient discoveries (code smells, metrics, suggestions).
    /// JetStream: ephemeral, 1h TTL, memory-backed.
    /// NATS subject: ancplua.storm.*
    /// </summary>
    Storm
}
