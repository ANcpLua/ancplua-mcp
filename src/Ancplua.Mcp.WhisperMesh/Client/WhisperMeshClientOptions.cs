namespace Ancplua.Mcp.WhisperMesh.Client;

/// <summary>
/// Configuration options for WhisperMeshClient.
/// </summary>
public sealed class WhisperMeshClientOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "WhisperMesh";

    /// <summary>
    /// NATS server URL.
    /// Default: nats://localhost:4222
    /// </summary>
    /// <remarks>
    /// CA1056 is suppressed because NATS.Client library expects a string URL, not Uri.
    /// </remarks>
#pragma warning disable CA1056 // URI properties should not be strings - NATS library expects string
    public string NatsUrl { get; set; } = "nats://localhost:4222";
#pragma warning restore CA1056

    /// <summary>
    /// Optional NATS authentication token.
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Optional NATS credentials file path.
    /// </summary>
    public string? CredentialsFile { get; set; }

    /// <summary>
    /// Stream name prefix for WhisperMesh streams.
    /// Default: WHISPERMESH
    /// </summary>
    public string StreamPrefix { get; set; } = "WHISPERMESH";

    /// <summary>
    /// Subject prefix for WhisperMesh subjects.
    /// Default: ancplua
    /// </summary>
    public string SubjectPrefix { get; set; } = "ancplua";

    /// <summary>
    /// Lightning stream retention duration (hours).
    /// Default: 24 hours per spec.
    /// </summary>
    public int LightningRetentionHours { get; set; } = 24;

    /// <summary>
    /// Storm stream retention duration (hours).
    /// Default: 1 hour per spec.
    /// </summary>
    public int StormRetentionHours { get; set; } = 1;

    /// <summary>
    /// Connection timeout in seconds.
    /// Default: 5 seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum reconnect attempts.
    /// Default: 10.
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    /// Reconnect wait time in milliseconds.
    /// Default: 2000ms (2 seconds).
    /// </summary>
    public int ReconnectWaitMs { get; set; } = 2000;

    /// <summary>
    /// Enable JetStream auto-provisioning of streams.
    /// Default: true (for local dev).
    /// </summary>
    public bool AutoProvisionStreams { get; set; } = true;

    /// <summary>
    /// Consumer durable name prefix.
    /// Default: whispermesh-consumer
    /// </summary>
    public string ConsumerDurablePrefix { get; set; } = "whispermesh-consumer";
}
