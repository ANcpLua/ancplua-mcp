# Spec-0110: WhisperMesh Phase 1 Implementation

## Overview

Implementation specification for WhisperMesh Phase 1 per ADR-0107 and Issue #41. Defines concrete interfaces, DTOs, and MCP tools for Dolphin Pod workflow integration.

## Scope

| In Scope | Out of Scope |
|----------|--------------|
| IWhisperMeshClient abstraction | Multi-cluster NATS federation |
| NatsWhisperMeshClient implementation | Persistent storage of whispers |
| WhisperAggregator service | Complex ML-based deduplication |
| MCP tools for emit/subscribe | Cross-repo skill integration |
| Local NATS docker setup | Production NATS deployment |

## Prerequisites

- Phase 0 complete (PR #39 merged)
- NATS packages in Directory.Packages.props (NATS.Client 2.5.1)
- Discovery types: `ArchitectureViolation`, `ImplementationIssue`, `CodeLocation`
- Protocol spec: `spec-whispermesh-protocol.md`

---

## 1. WhisperMesh Client Interface

### 1.1 IWhisperMeshClient

```csharp
namespace Ancplua.Mcp.WhisperMesh;

/// <summary>
/// Client for publishing and subscribing to WhisperMesh discoveries.
/// </summary>
public interface IWhisperMeshClient : IAsyncDisposable
{
    /// <summary>
    /// Publishes a discovery to WhisperMesh.
    /// </summary>
    /// <typeparam name="TDiscovery">Discovery type (must have [JsonPropertyName("type")])</typeparam>
    /// <param name="tier">Urgency tier (Lightning = critical, Storm = ambient)</param>
    /// <param name="topic">Dot-separated topic (e.g., "architecture", "security.cve")</param>
    /// <param name="discovery">The discovery payload</param>
    /// <param name="metadata">Optional context metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EmitAsync<TDiscovery>(
        WhisperTier tier,
        string topic,
        TDiscovery discovery,
        WhisperMetadata? metadata = null,
        CancellationToken cancellationToken = default) where TDiscovery : class;

    /// <summary>
    /// Subscribes to discoveries on a tier/topic.
    /// </summary>
    /// <typeparam name="TDiscovery">Expected discovery type</typeparam>
    /// <param name="tier">Tier to subscribe to</param>
    /// <param name="topic">Topic pattern (supports wildcards: *, >)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of whisper envelopes</returns>
    IAsyncEnumerable<WhisperEnvelope<TDiscovery>> SubscribeAsync<TDiscovery>(
        WhisperTier tier,
        string topic,
        CancellationToken cancellationToken = default) where TDiscovery : class;

    /// <summary>
    /// Whether the client is connected to NATS.
    /// </summary>
    bool IsConnected { get; }
}
```

### 1.2 WhisperEnvelope

```csharp
/// <summary>
/// Envelope wrapping a discovery with routing and tracing metadata.
/// </summary>
public sealed record WhisperEnvelope<TDiscovery> where TDiscovery : class
{
    /// <summary>Unique message ID (UUIDv4)</summary>
    public required Guid MessageId { get; init; }

    /// <summary>Agent that emitted this whisper</summary>
    public required string Agent { get; init; }

    /// <summary>Urgency tier</summary>
    public required WhisperTier Tier { get; init; }

    /// <summary>Topic category</summary>
    public required string Topic { get; init; }

    /// <summary>Normalized severity (0.0 - 1.0)</summary>
    public required double Severity { get; init; }

    /// <summary>UTC timestamp when emitted</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>The discovery payload</summary>
    public required TDiscovery Discovery { get; init; }

    /// <summary>Optional context metadata</summary>
    public WhisperMetadata? Metadata { get; init; }
}
```

---

## 2. NatsWhisperMeshClient Implementation

### 2.1 Configuration

```csharp
public sealed class WhisperMeshOptions
{
    /// <summary>NATS server URL (default: nats://localhost:4222)</summary>
    public string NatsUrl { get; set; } = "nats://localhost:4222";

    /// <summary>Agent identity for this client</summary>
    public required string AgentId { get; set; }

    /// <summary>JetStream stream name for Lightning tier</summary>
    public string LightningStreamName { get; set; } = "WHISPERMESH_LIGHTNING";

    /// <summary>JetStream stream name for Storm tier</summary>
    public string StormStreamName { get; set; } = "WHISPERMESH_STORM";

    /// <summary>Lightning retention (default: 24 hours)</summary>
    public TimeSpan LightningRetention { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Storm retention (default: 1 hour)</summary>
    public TimeSpan StormRetention { get; set; } = TimeSpan.FromHours(1);
}
```

### 2.2 appsettings.json Schema

```json
{
  "WhisperMesh": {
    "NatsUrl": "nats://localhost:4222",
    "AgentId": "ancplua-workstation",
    "LightningStreamName": "WHISPERMESH_LIGHTNING",
    "StormStreamName": "WHISPERMESH_STORM",
    "LightningRetentionHours": 24,
    "StormRetentionHours": 1
  }
}
```

### 2.3 DI Registration

```csharp
// In ServiceDefaults or WorkstationServer Program.cs
services.Configure<WhisperMeshOptions>(
    configuration.GetSection("WhisperMesh"));

services.AddSingleton<IWhisperMeshClient, NatsWhisperMeshClient>();
```

---

## 3. WhisperAggregator Service

### 3.1 Interface

```csharp
public interface IWhisperAggregator
{
    /// <summary>
    /// Aggregates discoveries from multiple sources with deduplication.
    /// </summary>
    Task<AggregatedWhisperReport> AggregateAsync(
        AggregationRequest request,
        CancellationToken cancellationToken = default);
}
```

### 3.2 Request/Response DTOs

```csharp
public sealed record AggregationRequest
{
    /// <summary>Tiers to include (null = all)</summary>
    public IReadOnlyList<WhisperTier>? Tiers { get; init; }

    /// <summary>Topic patterns to include (null = all)</summary>
    public IReadOnlyList<string>? Topics { get; init; }

    /// <summary>Time window to aggregate (default: last 5 minutes)</summary>
    public TimeSpan TimeWindow { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Maximum discoveries to return</summary>
    public int MaxItems { get; init; } = 100;
}

public sealed record AggregatedWhisperReport
{
    /// <summary>Deduplicated discoveries sorted by tier/severity</summary>
    public required IReadOnlyList<AggregatedDiscovery> Discoveries { get; init; }

    /// <summary>Count per tier</summary>
    public required IReadOnlyDictionary<WhisperTier, int> CountsByTier { get; init; }

    /// <summary>Count per agent</summary>
    public required IReadOnlyDictionary<string, int> CountsByAgent { get; init; }

    /// <summary>Total before deduplication</summary>
    public required int TotalBeforeDedup { get; init; }

    /// <summary>Total after deduplication</summary>
    public required int TotalAfterDedup { get; init; }

    /// <summary>Aggregation timestamp</summary>
    public required DateTimeOffset GeneratedAt { get; init; }
}

public sealed record AggregatedDiscovery
{
    /// <summary>Discovery type name</summary>
    public required string Type { get; init; }

    /// <summary>Location (if applicable)</summary>
    public CodeLocation? Location { get; init; }

    /// <summary>Highest severity from duplicates</summary>
    public required double Severity { get; init; }

    /// <summary>Tier</summary>
    public required WhisperTier Tier { get; init; }

    /// <summary>Agents that reported this</summary>
    public required IReadOnlyList<string> ReportedBy { get; init; }

    /// <summary>First seen timestamp</summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>Raw discovery JSON</summary>
    public required JsonElement Discovery { get; init; }
}
```

### 3.3 Deduplication Strategy

Discoveries are considered duplicates if:
1. Same `Type` (e.g., "ArchitectureViolation")
2. Same `CodeLocation` (file path + line number)
3. Same category/rule (from discovery payload)

When merging duplicates:
- Keep highest severity
- Combine all reporting agents
- Keep earliest timestamp

---

## 4. MCP Tools

### 4.1 WhisperAggregatorTools

Location: `src/Ancplua.Mcp.WorkstationServer/Tools/WhisperAggregatorTools.cs`

```csharp
[McpServerToolType]
public class WhisperAggregatorTools
{
    private readonly IWhisperAggregator _aggregator;

    public WhisperAggregatorTools(IWhisperAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    /// <summary>
    /// Aggregates WhisperMesh discoveries from multiple agents.
    /// </summary>
    [McpServerTool]
    [Description("Aggregates and deduplicates WhisperMesh discoveries")]
    public async Task<AggregatedWhisperReport> AggregateDiscoveries(
        [Description("Tiers to include (Lightning, Storm, or both)")]
        string[]? tiers = null,
        [Description("Topic patterns (e.g., 'architecture', 'security.*')")]
        string[]? topics = null,
        [Description("Time window in minutes (default: 5)")]
        int timeWindowMinutes = 5,
        [Description("Maximum items to return")]
        int maxItems = 100,
        CancellationToken cancellationToken = default)
    {
        var request = new AggregationRequest
        {
            Tiers = tiers?.Select(t => Enum.Parse<WhisperTier>(t, ignoreCase: true)).ToList(),
            Topics = topics?.ToList(),
            TimeWindow = TimeSpan.FromMinutes(timeWindowMinutes),
            MaxItems = maxItems
        };

        return await _aggregator.AggregateAsync(request, cancellationToken);
    }
}
```

### 4.2 Tool Contract (tool-contracts.md entry)

```markdown
### WhisperAggregatorTools.AggregateDiscoveries
- **Input**: `{ tiers?: string[], topics?: string[], timeWindowMinutes?: int, maxItems?: int }`
- **Output**: `AggregatedWhisperReport { discoveries, countsByTier, countsByAgent, totalBeforeDedup, totalAfterDedup, generatedAt }`
- **Side effects**: Reads from NATS JetStream (no writes)
```

---

## 5. Docker Infrastructure

### 5.1 docker-compose.yml Addition

```yaml
services:
  nats:
    image: nats:2.10-alpine
    container_name: whispermesh-nats
    ports:
      - "4222:4222"   # Client connections
      - "8222:8222"   # HTTP monitoring
    command: ["--jetstream", "--store_dir=/data", "-m", "8222"]
    volumes:
      - nats-data:/data
    healthcheck:
      test: ["CMD", "wget", "-q", "--spider", "http://localhost:8222/healthz"]
      interval: 5s
      timeout: 3s
      retries: 3

volumes:
  nats-data:
```

---

## 6. Testing Strategy

### 6.1 Unit Tests (No NATS)

- `WhisperAggregatorTests.cs`
  - Deduplication by CodeLocation
  - Severity merging (highest wins)
  - Tier-based sorting (Lightning first)
  - Agent attribution

### 6.2 Integration Tests (Testcontainers)

- `WhisperMeshIntegrationTests.cs`
  - NATS container setup
  - Emit → Subscribe round-trip
  - JetStream message persistence
  - Multi-agent scenario

---

## 7. Implementation Checklist

- [ ] Create `IWhisperMeshClient` interface
- [ ] Implement `NatsWhisperMeshClient`
- [ ] Create `WhisperMeshOptions` configuration
- [ ] Add NATS to docker-compose.yml
- [ ] Add config to appsettings.json
- [ ] Implement `WhisperAggregator` with deduplication
- [ ] Create `WhisperAggregatorTools` MCP class
- [ ] Wire services via DI in WorkstationServer
- [ ] Write unit tests for aggregation
- [ ] Write integration tests with Testcontainers
- [ ] Update tool-contracts.md
- [ ] Update CHANGELOG.md

---

## References

- ADR-0107: WhisperMesh Protocol Adoption
- spec-whispermesh-protocol.md: Protocol specification
- Issue #41: Phase 1 requirements
- PR #39: Phase 0 implementation
