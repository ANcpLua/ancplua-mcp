using System.Runtime.CompilerServices;
using System.Text.Json;
using Ancplua.Mcp.WhisperMesh.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Ancplua.Mcp.WhisperMesh.Client;

/// <summary>
/// NATS JetStream implementation of IWhisperMeshClient.
/// Handles pub/sub for WhisperMesh protocol via NATS JetStream.
/// </summary>
public sealed partial class NatsWhisperMeshClient : IWhisperMeshClient
{
    private readonly WhisperMeshClientOptions _options;
    private readonly ILogger<NatsWhisperMeshClient> _logger;
    private readonly NatsConnection _natsConnection;
    private readonly NatsJSContext _jetStreamContext;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsWhisperMeshClient"/> class.
    /// </summary>
    /// <param name="options">Client configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public NatsWhisperMeshClient(
        IOptions<WhisperMeshClientOptions> options,
        ILogger<NatsWhisperMeshClient> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure NATS connection (NATS.Client 2.x API)
        var natsOpts = new NatsOpts
        {
            Url = _options.NatsUrl,
            ConnectTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds),
            MaxReconnectRetry = _options.MaxReconnectAttempts,
            AuthOpts = BuildAuthOpts(),
            Name = "WhisperMeshClient"
        };

        _natsConnection = new NatsConnection(natsOpts);
        _jetStreamContext = new NatsJSContext(_natsConnection);

        LogClientInitialized(_options.NatsUrl);
    }

    /// <inheritdoc />
    public bool IsConnected => _natsConnection.ConnectionState == NatsConnectionState.Open;

    /// <inheritdoc />
    public async Task<EmitResult> EmitAsync<TDiscovery>(
        WhisperMessage message,
        CancellationToken cancellationToken = default)
        where TDiscovery : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            // Validate message
            var validation = message.Validate();
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors);
                LogInvalidMessage(errors);
                return EmitResult.Failed($"Validation failed: {errors}");
            }

            // Ensure stream exists (if auto-provisioning enabled)
            if (_options.AutoProvisionStreams)
            {
                await EnsureStreamExistsAsync(message.Tier, cancellationToken).ConfigureAwait(false);
            }

            // Build NATS subject
            var subject = BuildSubject(message.Tier, message.Topic);

            // Serialize message
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            // Publish to JetStream with message ID for deduplication
            var publishAck = await _jetStreamContext.PublishAsync(
                subject: subject,
                data: data,
                opts: new NatsJSPubOpts
                {
                    MsgId = message.MessageId,
                    ExpectedStream = GetStreamName(message.Tier)
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            LogMessageEmitted(message.MessageId, subject, publishAck.Seq);

            return EmitResult.Succeeded(message.MessageId);
        }
        catch (NatsException ex)
        {
            LogEmitFailed(ex, message.MessageId);
            return EmitResult.Failed($"Emit failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<WhisperMessage> SubscribeAsync(
        WhisperTier tier,
        string topicPattern,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Ensure stream exists
        if (_options.AutoProvisionStreams)
        {
            await EnsureStreamExistsAsync(tier, cancellationToken).ConfigureAwait(false);
        }

        // Build filter subject pattern
        var filterSubject = BuildSubject(tier, topicPattern);

        // Create ephemeral consumer for this subscription (NATS.Client 2.x API)
        var consumerConfig = new ConsumerConfig($"{_options.ConsumerDurablePrefix}-{Guid.NewGuid():N}")
        {
            FilterSubject = filterSubject,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            AckWait = TimeSpan.FromSeconds(30),
            MaxDeliver = 1,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All
        };

        var consumer = await _jetStreamContext.CreateOrUpdateConsumerAsync(
            stream: GetStreamName(tier),
            config: consumerConfig,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        LogSubscribed(tier.ToString(), filterSubject);

        // Stream messages - refactored to avoid yield in try-catch
        await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var processedMessage = await ProcessMessageAsync(msg, cancellationToken).ConfigureAwait(false);
            if (processedMessage is not null)
            {
                yield return processedMessage;
            }
        }
    }

    /// <summary>
    /// Processes a single NATS message and returns the deserialized WhisperMessage.
    /// Returns null if the message should be skipped (expired, invalid, etc.).
    /// </summary>
    private async Task<WhisperMessage?> ProcessMessageAsync(
        NatsJSMsg<byte[]> msg,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(msg.Data!);
            var whisperMessage = JsonSerializer.Deserialize<WhisperMessage>(json, _jsonOptions);

            if (whisperMessage is null)
            {
                LogNullDeserialization(msg.Subject);
                await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return null;
            }

            // Filter expired messages
            if (whisperMessage.IsExpired)
            {
                LogSkippingExpired(whisperMessage.MessageId, whisperMessage.ExpiresAt);
                await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return null;
            }

            // Acknowledge message after successful processing
            await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return whisperMessage;
        }
        catch (JsonException ex)
        {
            LogDeserializationFailed(ex, msg.Subject);
            // Acknowledge malformed messages to prevent redelivery
            await msg.AckAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (NatsException ex)
        {
            LogProcessingError(ex, "unknown");
            // Negative acknowledge to trigger redelivery
            await msg.NakAsync(delay: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken).ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>
    /// Ensures that the JetStream stream for the given tier exists.
    /// Creates it if auto-provisioning is enabled.
    /// </summary>
    private async Task EnsureStreamExistsAsync(WhisperTier tier, CancellationToken cancellationToken)
    {
        var streamName = GetStreamName(tier);
        var streamSubjects = new[] { BuildSubject(tier, ">") };

        try
        {
            // Try to get existing stream (NATS.Client 2.x API)
            await _jetStreamContext.GetStreamAsync(streamName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            // Stream doesn't exist, create it
            var retentionHours = tier == WhisperTier.Lightning
                ? _options.LightningRetentionHours
                : _options.StormRetentionHours;

            // NATS.Client 2.x: StreamConfig requires name and subjects in constructor
            var streamConfig = new StreamConfig(streamName, streamSubjects)
            {
                Retention = StreamConfigRetention.Limits,
                MaxAge = TimeSpan.FromHours(retentionHours),
                Storage = tier == WhisperTier.Lightning
                    ? StreamConfigStorage.File
                    : StreamConfigStorage.Memory,
                Discard = StreamConfigDiscard.Old,
                MaxMsgsPerSubject = 10000,
                DuplicateWindow = TimeSpan.FromMinutes(5)
            };

            await _jetStreamContext.CreateStreamAsync(streamConfig, cancellationToken).ConfigureAwait(false);

            LogStreamCreated(streamName, retentionHours);
        }
    }

    /// <summary>
    /// Builds the NATS subject for a given tier and topic.
    /// Format: {prefix}.{tier}.{topic}
    /// Example: ancplua.lightning.security.cve
    /// </summary>
    /// <remarks>
    /// Uses lowercase for tier names as per NATS subject naming conventions.
    /// CA1308 is suppressed because lowercase is required for NATS subjects.
    /// </remarks>
#pragma warning disable CA1308 // Normalize strings to uppercase - NATS subjects require lowercase
    private string BuildSubject(WhisperTier tier, string topic)
    {
        var tierName = tier.ToString().ToLowerInvariant();
        return $"{_options.SubjectPrefix}.{tierName}.{topic}";
    }
#pragma warning restore CA1308

    /// <summary>
    /// Gets the JetStream stream name for a given tier.
    /// Format: {prefix}_{TIER}
    /// Example: WHISPERMESH_LIGHTNING
    /// </summary>
    private string GetStreamName(WhisperTier tier)
    {
        var tierName = tier.ToString().ToUpperInvariant();
        return $"{_options.StreamPrefix}_{tierName}";
    }

    /// <summary>
    /// Builds NATS authentication options from configuration.
    /// </summary>
    private NatsAuthOpts? BuildAuthOpts()
    {
        if (!string.IsNullOrWhiteSpace(_options.AuthToken))
        {
            return new NatsAuthOpts { Token = _options.AuthToken };
        }

        if (!string.IsNullOrWhiteSpace(_options.CredentialsFile))
        {
            return new NatsAuthOpts { CredsFile = _options.CredentialsFile };
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // NatsJSContext in 2.x does not implement IAsyncDisposable
        // Only dispose the connection
        await _natsConnection.DisposeAsync().ConfigureAwait(false);

        LogClientDisposed();
    }

    // LoggerMessage delegates for high-performance logging (CA1848)
    [LoggerMessage(Level = LogLevel.Information, Message = "WhisperMeshClient initialized. NATS URL: {NatsUrl}")]
    private partial void LogClientInitialized(string natsUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid WhisperMessage: {Errors}")]
    private partial void LogInvalidMessage(string errors);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Emitted WhisperMessage: {MessageId} to {Subject} (seq: {Seq})")]
    private partial void LogMessageEmitted(string messageId, string subject, ulong seq);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to emit WhisperMessage: {MessageId}")]
    private partial void LogEmitFailed(Exception ex, string messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subscribed to {Tier} tier with filter: {Filter}")]
    private partial void LogSubscribed(string tier, string filter);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Deserialized null WhisperMessage from subject: {Subject}")]
    private partial void LogNullDeserialization(string subject);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping expired WhisperMessage: {MessageId} (expired at {ExpiresAt})")]
    private partial void LogSkippingExpired(string messageId, DateTimeOffset? expiresAt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize WhisperMessage from subject: {Subject}")]
    private partial void LogDeserializationFailed(Exception ex, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing WhisperMessage: {MessageId}")]
    private partial void LogProcessingError(Exception ex, string messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created JetStream stream: {StreamName} with {RetentionHours}h retention")]
    private partial void LogStreamCreated(string streamName, int retentionHours);

    [LoggerMessage(Level = LogLevel.Information, Message = "WhisperMeshClient disposed")]
    private partial void LogClientDisposed();
}
