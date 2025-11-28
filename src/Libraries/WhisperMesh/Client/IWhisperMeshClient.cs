using System.Runtime.CompilerServices;
using Ancplua.Mcp.Libraries.WhisperMesh.Models;

namespace Ancplua.Mcp.Libraries.WhisperMesh.Client;

/// <summary>
/// Client interface for publishing and subscribing to WhisperMesh messages via NATS JetStream.
/// Conforms to WhisperMesh Protocol Specification v1.0.
/// </summary>
public interface IWhisperMeshClient : IAsyncDisposable
{
    /// <summary>
    /// Emits a WhisperMesh message to the specified tier and topic.
    /// </summary>
    /// <typeparam name="TDiscovery">Discovery type (must have a "type" property for protocol compliance).</typeparam>
    /// <param name="message">The WhisperMessage to emit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with message ID.</returns>
    Task<EmitResult> EmitAsync<TDiscovery>(
        WhisperMessage message,
        CancellationToken cancellationToken = default)
        where TDiscovery : class;

    /// <summary>
    /// Subscribes to WhisperMesh messages on the specified tier and topic pattern.
    /// Returns an async stream of messages matching the subscription.
    /// </summary>
    /// <param name="tier">Tier to subscribe to (Lightning or Storm).</param>
    /// <param name="topicPattern">Topic pattern (supports NATS wildcards: * and >).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of WhisperMessage envelopes.</returns>
    IAsyncEnumerable<WhisperMessage> SubscribeAsync(
        WhisperTier tier,
        string topicPattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the client is connected to the NATS server.
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// Result of emitting a whisper message.
/// </summary>
public sealed record EmitResult
{
    /// <summary>
    /// Indicates whether the emit was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Message ID if successful, null otherwise.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Error message if emit failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static EmitResult Succeeded(string messageId) => new()
    {
        Success = true,
        MessageId = messageId
    };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static EmitResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
