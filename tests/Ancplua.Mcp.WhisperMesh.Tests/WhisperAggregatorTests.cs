using System.Runtime.CompilerServices;
using System.Text.Json;
using Ancplua.Mcp.Libraries.WhisperMesh.Client;
using Ancplua.Mcp.Libraries.WhisperMesh.Discoveries;
using Ancplua.Mcp.Libraries.WhisperMesh.Models;
using Ancplua.Mcp.Libraries.WhisperMesh.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ancplua.Mcp.WhisperMesh.Tests;

/// <summary>
/// Unit tests for WhisperAggregator (without NATS).
/// Tests deduplication and sorting logic using a mock WhisperMeshClient.
/// </summary>
#pragma warning disable CA2000 // Test cleanup handled by xUnit
public class WhisperAggregatorTests
{
    [Fact]
    public async Task AggregateDiscoveries_DeduplicatesBySameLocation()
    {
        // Arrange
        var client = new MockWhisperMeshClient();
        var aggregator = new WhisperAggregator(client, NullLogger<WhisperAggregator>.Instance);

        // Two discoveries at same location, different severity
        var location = new CodeLocation
        {
            File = "src/Processor.cs",
            Line = 42,
            Symbol = "ProcessData()"
        };

        var discovery1 = CreateArchitectureViolation(location, severity: 0.7);
        var discovery2 = CreateArchitectureViolation(location, severity: 0.9);

        client.AddMessage(CreateWhisperMessage("agent1", WhisperTier.Lightning, "architecture", discovery1, 0.7));
        client.AddMessage(CreateWhisperMessage("agent2", WhisperTier.Lightning, "architecture", discovery2, 0.9));

        var request = new AggregationRequest
        {
            Tiers = [WhisperTier.Lightning],
            TopicPatterns = ["architecture"],
            TimeWindowMinutes = 5,
            MinSeverity = 0.0
        };

        // Act
        var report = await aggregator.AggregateDiscoveriesAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, report.TotalCount);
        Assert.Equal(1, report.DeduplicatedCount);
        Assert.Equal(0.9, report.Discoveries[0].Severity); // Higher severity kept
    }

    [Fact]
    public async Task AggregateDiscoveries_SortsByTierThenSeverity()
    {
        // Arrange
        var client = new MockWhisperMeshClient();
        var aggregator = new WhisperAggregator(client, NullLogger<WhisperAggregator>.Instance);

        var location1 = new CodeLocation { File = "a.cs", Line = 1, Symbol = "A()" };
        var location2 = new CodeLocation { File = "b.cs", Line = 2, Symbol = "B()" };
        var location3 = new CodeLocation { File = "c.cs", Line = 3, Symbol = "C()" };

        // Add discoveries: Storm-high, Lightning-low, Lightning-high
        client.AddMessage(CreateWhisperMessage("agent1", WhisperTier.Storm, "perf", CreateImplIssue(location1, 0.9), 0.9));
        client.AddMessage(CreateWhisperMessage("agent2", WhisperTier.Lightning, "security", CreateImplIssue(location2, 0.5), 0.5));
        client.AddMessage(CreateWhisperMessage("agent3", WhisperTier.Lightning, "security", CreateImplIssue(location3, 0.95), 0.95));

        var request = new AggregationRequest
        {
            Tiers = [WhisperTier.Lightning, WhisperTier.Storm],
            TopicPatterns = [">"],
            TimeWindowMinutes = 5
        };

        // Act
        var report = await aggregator.AggregateDiscoveriesAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(3, report.DeduplicatedCount);
        // Lightning-high, Lightning-low, Storm-high
        Assert.Equal(WhisperTier.Lightning, report.Discoveries[0].Tier);
        Assert.Equal(0.95, report.Discoveries[0].Severity);
        Assert.Equal(WhisperTier.Lightning, report.Discoveries[1].Tier);
        Assert.Equal(0.5, report.Discoveries[1].Severity);
        Assert.Equal(WhisperTier.Storm, report.Discoveries[2].Tier);
        Assert.Equal(0.9, report.Discoveries[2].Severity);
    }

    [Fact]
    public async Task AggregateDiscoveries_FiltersBySeverity()
    {
        // Arrange
        var client = new MockWhisperMeshClient();
        var aggregator = new WhisperAggregator(client, NullLogger<WhisperAggregator>.Instance);

        var location1 = new CodeLocation { File = "a.cs", Line = 1, Symbol = "A()" };
        var location2 = new CodeLocation { File = "b.cs", Line = 2, Symbol = "B()" };

        client.AddMessage(CreateWhisperMessage("agent1", WhisperTier.Lightning, "code-quality", CreateImplIssue(location1, 0.3), 0.3));
        client.AddMessage(CreateWhisperMessage("agent2", WhisperTier.Lightning, "code-quality", CreateImplIssue(location2, 0.8), 0.8));

        var request = new AggregationRequest
        {
            Tiers = [WhisperTier.Lightning],
            TopicPatterns = ["code-quality"],
            MinSeverity = 0.5
        };

        // Act
        var report = await aggregator.AggregateDiscoveriesAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(1, report.DeduplicatedCount);
        Assert.Equal(0.8, report.Discoveries[0].Severity);
    }

    [Fact]
    public async Task AggregateDiscoveries_CountsBySeverityBucket()
    {
        // Arrange
        var client = new MockWhisperMeshClient();
        var aggregator = new WhisperAggregator(client, NullLogger<WhisperAggregator>.Instance);

        for (int i = 0; i < 10; i++)
        {
            var location = new CodeLocation { File = $"{i}.cs", Line = i, Symbol = $"Method{i}()" };
            var severity = i switch
            {
                < 3 => 0.3,  // Low (3)
                < 6 => 0.5,  // Medium (3)
                < 9 => 0.7,  // High (3)
                _ => 0.9     // Critical (1)
            };
            client.AddMessage(CreateWhisperMessage($"agent{i}", WhisperTier.Lightning, "test", CreateImplIssue(location, severity), severity));
        }

        var request = new AggregationRequest
        {
            Tiers = [WhisperTier.Lightning],
            TopicPatterns = ["test"]
        };

        // Act
        var report = await aggregator.AggregateDiscoveriesAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(10, report.DeduplicatedCount);
        Assert.Equal(1, report.CriticalCount);   // >= 0.8
        Assert.Equal(3, report.HighCount);       // 0.6 <= x < 0.8
        Assert.Equal(3, report.MediumCount);     // 0.4 <= x < 0.6
        Assert.Equal(3, report.LowCount);        // < 0.4
    }

    [Fact]
    public async Task AggregateDiscoveries_CountsByTier()
    {
        // Arrange
        var client = new MockWhisperMeshClient();
        var aggregator = new WhisperAggregator(client, NullLogger<WhisperAggregator>.Instance);

        for (int i = 0; i < 5; i++)
        {
            var location = new CodeLocation { File = $"{i}.cs", Line = i, Symbol = $"Method{i}()" };
            var tier = i % 2 == 0 ? WhisperTier.Lightning : WhisperTier.Storm;
            client.AddMessage(CreateWhisperMessage($"agent{i}", tier, "test", CreateImplIssue(location, 0.5), 0.5));
        }

        var request = new AggregationRequest
        {
            Tiers = [WhisperTier.Lightning, WhisperTier.Storm],
            TopicPatterns = ["test"]
        };

        // Act
        var report = await aggregator.AggregateDiscoveriesAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(5, report.DeduplicatedCount);
        Assert.Equal(3, report.LightningCount);
        Assert.Equal(2, report.StormCount);
    }

    [Fact]
    public async Task AggregateDiscoveries_CountsByAgent()
    {
        // Arrange
        var client = new MockWhisperMeshClient();
        var aggregator = new WhisperAggregator(client, NullLogger<WhisperAggregator>.Instance);

        for (int i = 0; i < 6; i++)
        {
            var location = new CodeLocation { File = $"{i}.cs", Line = i, Symbol = $"Method{i}()" };
            var agent = i < 3 ? "ARCH-Agent" : "IMPL-Agent";
            client.AddMessage(CreateWhisperMessage(agent, WhisperTier.Lightning, "test", CreateImplIssue(location, 0.5), 0.5));
        }

        var request = new AggregationRequest
        {
            Tiers = [WhisperTier.Lightning],
            TopicPatterns = ["test"]
        };

        // Act
        var report = await aggregator.AggregateDiscoveriesAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, report.AgentCounts.Count);
        Assert.Equal(3, report.AgentCounts["ARCH-Agent"]);
        Assert.Equal(3, report.AgentCounts["IMPL-Agent"]);
    }

    // Helper methods

    private static WhisperMessage CreateWhisperMessage(
        string agent,
        WhisperTier tier,
        string topic,
        object discovery,
        double severity)
    {
        var json = JsonSerializer.Serialize(discovery);
        var discoveryElement = JsonSerializer.Deserialize<JsonElement>(json);

        return new WhisperMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Agent = agent,
            Tier = tier,
            Topic = topic,
            Severity = severity,
            Discovery = discoveryElement,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ArchitectureViolation CreateArchitectureViolation(CodeLocation location, double severity)
    {
        return new ArchitectureViolation
        {
            Location = location,
            Rule = "ADR-006",
            Severity = severity,
            Finding = "Test architecture violation",
            Agent = "test-agent"
        };
    }

    private static ImplementationIssue CreateImplIssue(CodeLocation location, double severity)
    {
        return new ImplementationIssue
        {
            Location = location,
            Category = "security",
            Severity = severity,
            Finding = "Test implementation issue",
            Agent = "test-agent"
        };
    }
}

/// <summary>
/// Mock WhisperMeshClient for testing aggregation logic without NATS.
/// </summary>
internal sealed class MockWhisperMeshClient : IWhisperMeshClient
{
    private readonly List<WhisperMessage> _messages = [];

    public bool IsConnected => true;

    public void AddMessage(WhisperMessage message)
    {
        _messages.Add(message);
    }

    public Task<EmitResult> EmitAsync<TDiscovery>(WhisperMessage message, CancellationToken cancellationToken = default)
        where TDiscovery : class
    {
        _messages.Add(message);
        return Task.FromResult(EmitResult.Succeeded(message.MessageId));
    }

    public async IAsyncEnumerable<WhisperMessage> SubscribeAsync(
        WhisperTier tier,
        string topicPattern,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var message in _messages.Where(m => m.Tier == tier))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return message;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _messages.Clear();
        return ValueTask.CompletedTask;
    }
}
