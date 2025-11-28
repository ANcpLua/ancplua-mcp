using System.Text.Json.Serialization;

namespace Ancplua.Mcp.WhisperMesh.Discoveries;

/// <summary>
/// Discovery type for Dolphin Pod IMPL agent findings.
/// Represents implementation-level issues: security vulnerabilities, performance problems, correctness bugs.
/// Conforms to WhisperMesh Protocol Specification v1.0 §4.
/// </summary>
/// <remarks>
/// Example usage in Dolphin Pod PR review:
/// - SQL injection vulnerability
/// - Race condition in async code
/// - Memory leak in long-running process
/// - N+1 query problem
/// - Incorrect error handling
/// </remarks>
public sealed record ImplementationIssue
{
    /// <summary>
    /// Discovery type identifier (always "ImplementationIssue").
    /// Required by WhisperMesh Protocol Specification v1.0 §4.1.
    /// </summary>
    /// <remarks>
    /// CA1822 is suppressed because this property must be instance-based for JSON serialization.
    /// </remarks>
    [JsonPropertyName("type")]
#pragma warning disable CA1822 // Mark members as static - must be instance for JSON serialization
    public string Type => "ImplementationIssue";
#pragma warning restore CA1822

    /// <summary>
    /// Location of the issue in source code.
    /// </summary>
    [JsonPropertyName("location")]
    public required CodeLocation Location { get; init; }

    /// <summary>
    /// Issue category.
    /// Valid values: "security", "performance", "correctness", "reliability", "maintainability"
    /// </summary>
    [JsonPropertyName("category")]
    public required string Category { get; init; }

    /// <summary>
    /// Normalized severity score (0.0 = informational, 1.0 = critical).
    /// Security: 0.8-1.0 for exploitable, 0.5-0.7 for hardening
    /// Performance: 0.8-1.0 for user-visible, 0.3-0.5 for micro-optimizations
    /// Correctness: 0.8-1.0 for data corruption, 0.5-0.7 for edge cases
    /// </summary>
    [JsonPropertyName("severity")]
    public required double Severity { get; init; }

    /// <summary>
    /// Human-readable description of the issue.
    /// Example: "Potential SQL injection: user input concatenated into query without parameterization."
    /// </summary>
    [JsonPropertyName("finding")]
    public required string Finding { get; init; }

    /// <summary>
    /// Agent that emitted this discovery.
    /// Example: "IMPL-Agent", "Jules-Dolphin-IMPL", "CodeRabbit-Security"
    /// </summary>
    [JsonPropertyName("agent")]
    public required string Agent { get; init; }

    /// <summary>
    /// Optional: Suggested fix or remediation steps.
    /// Example: "Use parameterized queries: cmd.Parameters.AddWithValue(\"@userId\", userId)"
    /// </summary>
    [JsonPropertyName("suggestion")]
    public string? Suggestion { get; init; }
}
