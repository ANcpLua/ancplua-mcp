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
public sealed class NatsWhisperMeshClient : IWhisperMeshClient
{
    private readonly WhisperMeshClientOptions _options;
    private readonly ILogger<NatsWhisperMeshClient> _logger;
    private readonly NatsConnection _natsConnection;
    private readonly INatsJSContext _jetStreamContext;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public NatsWhisperMeshClient(
        IOptions<WhisperMeshClientOptions> options,
        ILogger<NatsWhisperMeshClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure NATS connection
        var natsOpts = new NatsOpts
        {
            Url = _options.NatsUrl,
            ConnectTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds),
            MaxReconnectRetry = _options.MaxReconnectAttempts,
            ReconnectWait = TimeSpan.FromMilliseconds(_options.ReconnectWaitMs),
            AuthOpts = BuildAuthOpts(),
            Name = "WhisperMeshClient"
        };

        _natsConnection = new NatsConnection(natsOpts);
        _jetStreamContext = new NatsJSContext(_natsConnection);

        _logger.LogInformation(
            "WhisperMeshClient initialized. NATS URL: {NatsUrl}",
            _options.NatsUrl);
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

        try
        {
            // Validate message
            var validation = message.Validate();
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors);
                _logger.LogWarning("Invalid WhisperMessage: {Errors}", errors);
                return EmitResult.Failed($"Validation failed: {errors}");
            }

            // Ensure stream exists (if auto-provisioning enabled)
            if (_options.AutoProvisionStreams)
            {
                await EnsureStreamExistsAsync(message.Tier, cancellationToken);
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
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Emitted WhisperMessage: {MessageId} to {Subject} (seq: {Seq})",
                message.MessageId,
                subject,
                publishAck.Seq);

            return EmitResult.Succeeded(message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit WhisperMessage: {MessageId}", message.MessageId);
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
            await EnsureStreamExistsAsync(tier, cancellationToken);
        }

        // Build filter subject pattern
        var filterSubject = BuildSubject(tier, topicPattern);

        // Create ephemeral consumer for this subscription
        var consumerConfig = new ConsumerConfig
        {
            Name = $"{_options.ConsumerDurablePrefix}-{Guid.NewGuid():N}",
            FilterSubject = filterSubject,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            AckWait = TimeSpan.FromSeconds(30),
            MaxDeliver = 1,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All
        };

        var consumer = await _jetStreamContext.CreateOrUpdateConsumerAsync(
            stream: GetStreamName(tier),
            config: consumerConfig,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Subscribed to {Tier} tier with filter: {Filter}",
            tier,
            filterSubject);

        // Stream messages
        await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            WhisperMessage? whisperMessage = null;
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(msg.Data!);
                whisperMessage = JsonSerializer.Deserialize<WhisperMessage>(json, _jsonOptions);

                if (whisperMessage == null)
                {
                    _logger.LogWarning("Deserialized null WhisperMessage from subject: {Subject}", msg.Subject);
                    await msg.AckAsync(cancellationToken: cancellationToken);
                    continue;
                }

                // Filter expired messages
                if (whisperMessage.IsExpired)
                {
                    _logger.LogDebug(
                        "Skipping expired WhisperMessage: {MessageId} (expired at {ExpiresAt})",
                        whisperMessage.MessageId,
                        whisperMessage.ExpiresAt);
                    await msg.AckAsync(cancellationToken: cancellationToken);
                    continue;
                }

                yield return whisperMessage;

                // Acknowledge message
                await msg.AckAsync(cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize WhisperMessage from subject: {Subject}", msg.Subject);
                // Acknowledge malformed messages to prevent redelivery
                await msg.AckAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhisperMessage: {MessageId}", whisperMessage?.MessageId ?? "unknown");
                // Negative acknowledge to trigger redelivery
                await msg.NakAsync(delay: TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
            }
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
            // Try to get existing stream
            await _jetStreamContext.GetStreamAsync(streamName, cancellationToken);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            // Stream doesn't exist, create it
            var retentionHours = tier == WhisperTier.Lightning
                ? _options.LightningRetentionHours
                : _options.StormRetentionHours;

            var streamConfig = new StreamConfig
            {
                Name = streamName,
                Subjects = streamSubjects,
                Retention = StreamConfigRetention.Limits,
                MaxAge = TimeSpan.FromHours(retentionHours),
                Storage = tier == WhisperTier.Lightning
                    ? StreamConfigStorage.File
                    : StreamConfigStorage.Memory,
                Discard = StreamConfigDiscard.Old,
                MaxMsgsPerSubject = 10000,
                DuplicateWindow = TimeSpan.FromMinutes(5)
            };

            await _jetStreamContext.CreateStreamAsync(streamConfig, cancellationToken);

            _logger.LogInformation(
                "Created JetStream stream: {StreamName} with {RetentionHours}h retention",
                streamName,
                retentionHours);
        }
    }

    /// <summary>
    /// Builds the NATS subject for a given tier and topic.
    /// Format: {prefix}.{tier}.{topic}
    /// Example: ancplua.lightning.security.cve
    /// </summary>
    private string BuildSubject(WhisperTier tier, string topic)
    {
        var tierName = tier.ToString().ToLowerInvariant();
        return $"{_options.SubjectPrefix}.{tierName}.{topic}";
    }

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

        try
        {
            await _jetStreamContext.DisposeAsync();
            await _natsConnection.DisposeAsync();

            _logger.LogInformation("WhisperMeshClient disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing WhisperMeshClient");
        }
    }
}
