using System.Text.Json.Serialization;

namespace Ancplua.Mcp.WhisperMesh.Discoveries;

/// <summary>
/// Discovery type for Dolphin Pod ARCH agent findings.
/// Represents violations of architectural rules, design patterns, or project conventions.
/// Conforms to WhisperMesh Protocol Specification v1.0 §4.
/// </summary>
/// <remarks>
/// Example usage in Dolphin Pod PR review:
/// - Missing ADR for new tool contract
/// - Breaking change without spec update
/// - Violation of CLAUDE.md guidelines
/// - Cross-repo contract incompatibility
/// </remarks>
public sealed record ArchitectureViolation
{
    /// <summary>
    /// Discovery type identifier (always "ArchitectureViolation").
    /// Required by WhisperMesh Protocol Specification v1.0 §4.1.
    /// </summary>
    /// <remarks>
    /// CA1822 is suppressed because this property must be instance-based for JSON serialization.
    /// </remarks>
    [JsonPropertyName("type")]
#pragma warning disable CA1822 // Mark members as static - must be instance for JSON serialization
    public string Type => "ArchitectureViolation";
#pragma warning restore CA1822

    /// <summary>
    /// Location of the violation in source code.
    /// </summary>
    [JsonPropertyName("location")]
    public required CodeLocation Location { get; init; }

    /// <summary>
    /// Rule or guideline violated.
    /// Examples: "CLAUDE.md#section-5.1", "ADR-006", "spec-003-csharp-features"
    /// </summary>
    [JsonPropertyName("rule")]
    public required string Rule { get; init; }

    /// <summary>
    /// Normalized severity score (0.0 = informational, 1.0 = critical).
    /// Typical: 0.8-1.0 for blocking issues, 0.5-0.7 for warnings.
    /// </summary>
    [JsonPropertyName("severity")]
    public required double Severity { get; init; }

    /// <summary>
    /// Human-readable description of the violation.
    /// Example: "Missing ADR for new tool contract. All tool changes require ADR per CLAUDE.md §2."
    /// </summary>
    [JsonPropertyName("finding")]
    public required string Finding { get; init; }

    /// <summary>
    /// Agent that emitted this discovery.
    /// Example: "ARCH-Agent", "Claude-Dolphin-ARCH"
    /// </summary>
    [JsonPropertyName("agent")]
    public required string Agent { get; init; }
}
