using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ancplua.Mcp.Libraries.WhisperMesh.Models;

/// <summary>
/// Represents a whisper message in the WhisperMesh protocol.
/// Conforms to WhisperMesh Protocol Specification v1.0.
/// </summary>
/// <remarks>
/// CA1062 is suppressed for init properties because validation happens in the Validate() method.
/// The 'required' modifier ensures non-null at construction time.
/// </remarks>
#pragma warning disable CA1062 // Validate arguments of public methods - required modifier ensures non-null, Validate() handles validation
public sealed record WhisperMessage
{
    /// <summary>
    /// Unique identifier for this whisper (UUIDv4). Used for deduplication.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init => field = value.Trim(); }

    /// <summary>
    /// Agent identity (alphanumeric, dash, underscore only). Max 64 chars.
    /// Examples: "RoslynMetricsServer", "ClaudeCode", "Rider-Agent"
    /// </summary>
    [JsonPropertyName("agent")]
    public required string Agent { get; init => field = value.Trim(); }

    /// <summary>
    /// Urgency tier: Lightning (critical) or Storm (ambient).
    /// </summary>
    [JsonPropertyName("tier")]
    [JsonConverter(typeof(JsonStringEnumConverter<WhisperTier>))]
    public required WhisperTier Tier { get; init; }

    /// <summary>
    /// Dot-separated category for routing (e.g., "code-quality", "security.cve").
    /// Lowercase alphanumeric with dots, dashes, underscores. Max 128 chars.
    /// </summary>
    /// <remarks>
    /// Uses lowercase per protocol specification.
    /// CA1308 is suppressed because lowercase is required for protocol compliance.
    /// </remarks>
    [JsonPropertyName("topic")]
#pragma warning disable CA1308 // Normalize strings to uppercase - protocol requires lowercase
    public required string Topic { get; init => field = value.Trim().ToLowerInvariant(); }
#pragma warning restore CA1308

    /// <summary>
    /// Normalized urgency score (0.0 = informational, 1.0 = critical).
    /// </summary>
    [JsonPropertyName("severity")]
    public required double Severity { get; init; }

    /// <summary>
    /// Optional human-readable summary. Max 1024 chars.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init => field = value?.Trim(); }

    /// <summary>
    /// Structured, type-specific discovery data. Must include "type" field.
    /// </summary>
    [JsonPropertyName("discovery")]
    public JsonElement? Discovery { get; init; }

    /// <summary>
    /// Context metadata for traceability (project, commit, tool version, etc.).
    /// </summary>
    [JsonPropertyName("metadata")]
    public WhisperMetadata? Metadata { get; init; }

    /// <summary>
    /// ISO 8601 UTC timestamp when whisper was created.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Optional correlation ID for distributed tracing.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional expiration timestamp. Consumers should ignore expired whispers.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Checks if this whisper has expired based on current time.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;

    /// <summary>
    /// Validates the whisper message against protocol requirements.
    /// </summary>
    /// <returns>Validation result with errors if invalid.</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // MessageId validation
        if (string.IsNullOrWhiteSpace(MessageId) || !Guid.TryParse(MessageId, out _))
        {
            errors.Add("MessageId must be a valid UUIDv4");
        }

        // Agent validation
        if (string.IsNullOrWhiteSpace(Agent) || Agent.Length > 64)
        {
            errors.Add("Agent must be 1-64 characters");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(Agent, "^[a-zA-Z0-9_-]+$"))
        {
            errors.Add("Agent must contain only alphanumeric, dash, underscore characters");
        }

        // Topic validation
        if (string.IsNullOrWhiteSpace(Topic) || Topic.Length > 128)
        {
            errors.Add("Topic must be 1-128 characters");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(Topic, "^[a-z0-9._-]+$"))
        {
            errors.Add("Topic must be lowercase alphanumeric with dots, dashes, underscores only");
        }

        // Severity validation
        if (Severity < 0.0 || Severity > 1.0)
        {
            errors.Add("Severity must be between 0.0 and 1.0");
        }

        // Message length validation
        if (Message?.Length > 1024)
        {
            errors.Add("Message must be max 1024 characters");
        }

        // Timestamp validation
        if (Timestamp > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            errors.Add("Timestamp cannot be more than 5 minutes in the future");
        }
        else if (Timestamp < DateTimeOffset.UtcNow.AddDays(-7))
        {
            errors.Add("Timestamp cannot be more than 7 days in the past");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.AsReadOnly());
    }
}

/// <summary>
/// Represents validation result for a whisper message.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(IReadOnlyList<string> errors) => new() { IsValid = false, Errors = errors };
}
#pragma warning restore CA1062
