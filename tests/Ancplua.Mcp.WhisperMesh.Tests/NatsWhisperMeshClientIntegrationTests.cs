using System.Text.Json;
using Ancplua.Mcp.WhisperMesh.Client;
using Ancplua.Mcp.WhisperMesh.Discoveries;
using Ancplua.Mcp.WhisperMesh.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Ancplua.Mcp.WhisperMesh.Tests;

/// <summary>
/// Integration tests for NatsWhisperMeshClient with real NATS server via Testcontainers.
/// Tests end-to-end publish/subscribe flow.
/// </summary>
public sealed class NatsWhisperMeshClientIntegrationTests : IAsyncLifetime
{
    private IContainer? _natsContainer;
    private string _natsUrl = string.Empty;

    public async Task InitializeAsync()
    {
        // Start NATS container with JetStream enabled
        _natsContainer = new ContainerBuilder()
            .WithImage("nats:latest")
            .WithPortBinding(4222, true)
            .WithCommand("--jetstream", "--store_dir=/data")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server is ready"))
            .Build();

        await _natsContainer.StartAsync().ConfigureAwait(false);

        // Get mapped port
        var mappedPort = _natsContainer.GetMappedPublicPort(4222);
        _natsUrl = $"nats://localhost:{mappedPort}";
    }

    public async Task DisposeAsync()
    {
        if (_natsContainer != null)
        {
            await _natsContainer.StopAsync().ConfigureAwait(false);
            await _natsContainer.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task EmitAsync_PublishesMessageToNats()
    {
        // Arrange
        var client = CreateClient();

        var location = new CodeLocation
        {
            File = "src/Test.cs",
            Line = 10,
            Symbol = "TestMethod()"
        };

        var violation = new ArchitectureViolation
        {
            Location = location,
            Rule = "ADR-001",
            Severity = 0.9,
            Finding = "Test violation",
            Agent = "test-agent"
        };

        var discoveryJson = JsonSerializer.Serialize(violation);
        var discoveryElement = JsonSerializer.Deserialize<JsonElement>(discoveryJson);

        var message = new WhisperMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Agent = "integration-test",
            Tier = WhisperTier.Lightning,
            Topic = "architecture",
            Severity = 0.9,
            Message = "Test message",
            Discovery = discoveryElement,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = await client.EmitAsync<ArchitectureViolation>(message);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(message.MessageId, result.MessageId);

        await client.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAsync_ReceivesPublishedMessages()
    {
        // Arrange
        var client = CreateClient();

        var location = new CodeLocation
        {
            File = "src/Test.cs",
            Line = 20,
            Symbol = "TestMethod2()"
        };

        var issue = new ImplementationIssue
        {
            Location = location,
            Category = "security",
            Severity = 0.95,
            Finding = "SQL injection vulnerability",
            Agent = "security-agent"
        };

        var discoveryJson = JsonSerializer.Serialize(issue);
        var discoveryElement = JsonSerializer.Deserialize<JsonElement>(discoveryJson);

        var message = new WhisperMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Agent = "integration-test-2",
            Tier = WhisperTier.Lightning,
            Topic = "security.cve",
            Severity = 0.95,
            Message = "Critical security issue",
            Discovery = discoveryElement,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Publish message
        await client.EmitAsync<ImplementationIssue>(message);

        // Give NATS time to process
        await Task.Delay(500);

        // Act - Subscribe and collect messages
        var receivedMessages = new List<WhisperMessage>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var received in client.SubscribeAsync(WhisperTier.Lightning, "security.*", cts.Token))
        {
            receivedMessages.Add(received);
            break; // We only expect one message
        }

        // Assert
        Assert.Single(receivedMessages);
        var receivedMessage = receivedMessages[0];
        Assert.Equal(message.MessageId, receivedMessage.MessageId);
        Assert.Equal(message.Agent, receivedMessage.Agent);
        Assert.Equal(message.Topic, receivedMessage.Topic);
        Assert.Equal(message.Severity, receivedMessage.Severity);

        await client.DisposeAsync();
    }

    [Fact]
    public async Task EmitAsync_DeduplicatesMessagesByMessageId()
    {
        // Arrange
        var client = CreateClient();

        var location = new CodeLocation
        {
            File = "src/Test.cs",
            Line = 30,
            Symbol = "TestMethod3()"
        };

        var violation = new ArchitectureViolation
        {
            Location = location,
            Rule = "ADR-002",
            Severity = 0.8,
            Finding = "Test deduplication",
            Agent = "test-agent"
        };

        var discoveryJson = JsonSerializer.Serialize(violation);
        var discoveryElement = JsonSerializer.Deserialize<JsonElement>(discoveryJson);

        var messageId = Guid.NewGuid().ToString();

        var message1 = new WhisperMessage
        {
            MessageId = messageId,
            Agent = "integration-test-3",
            Tier = WhisperTier.Storm,
            Topic = "code-quality",
            Severity = 0.8,
            Discovery = discoveryElement,
            Timestamp = DateTimeOffset.UtcNow
        };

        var message2 = new WhisperMessage
        {
            MessageId = messageId, // Same message ID
            Agent = "integration-test-3",
            Tier = WhisperTier.Storm,
            Topic = "code-quality",
            Severity = 0.8,
            Discovery = discoveryElement,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act - Emit same message twice
        var result1 = await client.EmitAsync<ArchitectureViolation>(message1);
        var result2 = await client.EmitAsync<ArchitectureViolation>(message2);

        // Give NATS time to process
        await Task.Delay(500);

        // Subscribe and collect messages
        var receivedMessages = new List<WhisperMessage>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var received in client.SubscribeAsync(WhisperTier.Storm, "code-quality", cts.Token))
        {
            receivedMessages.Add(received);

            // Wait a bit to see if duplicate arrives
            await Task.Delay(1000);
            break;
        }

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        // NATS JetStream should deduplicate based on MsgId
        Assert.Single(receivedMessages);

        await client.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAsync_FiltersExpiredMessages()
    {
        // Arrange
        var client = CreateClient();

        var location = new CodeLocation
        {
            File = "src/Test.cs",
            Line = 40,
            Symbol = "TestMethod4()"
        };

        var issue = new ImplementationIssue
        {
            Location = location,
            Category = "performance",
            Severity = 0.6,
            Finding = "N+1 query detected",
            Agent = "perf-agent"
        };

        var discoveryJson = JsonSerializer.Serialize(issue);
        var discoveryElement = JsonSerializer.Deserialize<JsonElement>(discoveryJson);

        // Create expired message
        var expiredMessage = new WhisperMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Agent = "integration-test-4",
            Tier = WhisperTier.Storm,
            Topic = "performance",
            Severity = 0.6,
            Discovery = discoveryElement,
            Timestamp = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-10) // Already expired
        };

        // Create valid message
        var validMessage = new WhisperMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Agent = "integration-test-4",
            Tier = WhisperTier.Storm,
            Topic = "performance",
            Severity = 0.7,
            Discovery = discoveryElement,
            Timestamp = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        // Act
        await client.EmitAsync<ImplementationIssue>(expiredMessage);
        await client.EmitAsync<ImplementationIssue>(validMessage);

        await Task.Delay(500);

        // Subscribe
        var receivedMessages = new List<WhisperMessage>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var received in client.SubscribeAsync(WhisperTier.Storm, "performance", cts.Token))
        {
            receivedMessages.Add(received);

            // Wait to see if expired message arrives (it shouldn't)
            await Task.Delay(1000);
            break;
        }

        // Assert - Only non-expired message should be received
        Assert.Single(receivedMessages);
        Assert.Equal(validMessage.MessageId, receivedMessages[0].MessageId);

        await client.DisposeAsync();
    }

    private NatsWhisperMeshClient CreateClient()
    {
        var options = Options.Create(new WhisperMeshClientOptions
        {
            NatsUrl = _natsUrl,
            AutoProvisionStreams = true,
            LightningRetentionHours = 24,
            StormRetentionHours = 1
        });

        return new NatsWhisperMeshClient(options, NullLogger<NatsWhisperMeshClient>.Instance);
    }
}
