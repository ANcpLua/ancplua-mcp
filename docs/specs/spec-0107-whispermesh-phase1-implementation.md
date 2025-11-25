# spec-0107: WhisperMesh Phase 1 - Dolphin Pod Integration

## Status
Accepted

## Context

WhisperMesh Phase 0 (PR #39, ADR-0107) established the foundation:
- `WhisperTier` enum (Lightning/Storm)
- `WhisperMetadata` for attribution
- Discovery types: `ArchitectureViolation`, `ImplementationIssue`, `CodeLocation`
- JSON serialization with 8 passing tests
- NATS packages in `Directory.Packages.props`

**Problem**: Dolphin Pod multi-agent workflow currently runs sequentially. Phase 1 enables parallel execution where multiple Claude instances (Architecture, Security, Performance) emit discoveries that are aggregated and deduplicated into a unified report.

**References**:
- ADR-0107: WhisperMesh Protocol Adoption
- spec-whispermesh-protocol.md: Protocol specification v1.0
- Issue #41: WhisperMesh Phase 1 requirements

---

## Specification

### Overview

Phase 1 delivers the minimum viable infrastructure for WhisperMesh-powered Dolphin Pod reviews:

1. **Client Infrastructure**: NATS JetStream pub/sub for discovery emission/subscription
2. **Aggregator Service**: Deduplication and tier-based sorting of discoveries
3. **MCP Tools**: WorkstationServer tools for consuming aggregated discoveries
4. **Integration Tests**: Testcontainers-based validation with real NATS
5. **Documentation**: Tool contracts, configuration examples, architecture updates

---

### Requirements

#### Functional Requirements

**[FR-1] WhisperMesh Client**
- MUST provide `IWhisperMeshClient` abstraction independent of transport
- MUST implement `NatsWhisperMeshClient` using NATS JetStream
- MUST support `EmitAsync<TDiscovery>` for publishing discoveries
- MUST support `SubscribeAsync<TDiscovery>` for consuming discoveries with metadata
- MUST handle reconnection and transient failures via Polly resilience

**[FR-2] Discovery Emission**
- MUST serialize discoveries to JSON conforming to WhisperMesh Protocol v1.0
- MUST include `WhisperMetadata` (project, repository, commit, branch, toolVersion)
- MUST publish to subject pattern: `ancplua.{tier}.{topic}` (e.g., `ancplua.storm.code-quality`)
- MUST set NATS `Nats-Msg-Id` header to `discovery.MessageId` for deduplication
- MUST emit OpenTelemetry span for each EmitAsync call

**[FR-3] Discovery Subscription**
- MUST return `IAsyncEnumerable<WhisperEnvelope<TDiscovery>>` from SubscribeAsync
- MUST support wildcard subscriptions (e.g., `ancplua.lightning.*`, `ancplua.storm.security.*`)
- MUST deserialize JSON to strongly-typed discovery DTOs
- MUST include envelope metadata: `ReceivedAt`, `Subject`, `MessageId`
- MUST handle deserialization failures gracefully (log and skip invalid messages)

**[FR-4] WhisperAggregator Service**
- MUST subscribe to multiple topics/tiers simultaneously
- MUST deduplicate discoveries by `CodeLocation + DiscoveryType + Agent`
- MUST sort results by `WhisperTier` (Lightning first) then `Severity` (descending)
- MUST support time-window filtering (e.g., "last 5 minutes of discoveries")
- MUST support max-items limit to prevent unbounded memory growth

**[FR-5] MCP Tool: AggregateDiscoveries**
- MUST expose `AggregateDiscoveries` tool in WorkstationServer
- MUST accept request: `{ tiers: string[], topics: string[], timeWindowMinutes?: int, maxItems?: int }`
- MUST return `AggregatedWhisperReport` with:
  - `discoveries: TDiscovery[]` (deduplicated, sorted)
  - `tierCounts: { Lightning: int, Storm: int }`
  - `agentBreakdown: { [agent: string]: int }` (optional, counts per agent)
  - `totalReceived: int`, `totalDeduplicated: int`
- MUST follow existing MCP tool conventions (POCO models, CancellationToken support)

**[FR-6] Configuration**
- MUST support NATS connection URL via `appsettings.json` and environment variable `NATS_URL`
- MUST default to `nats://localhost:4222` for local development
- MUST support JetStream stream configuration:
  - Lightning: `ancplua-lightning`, subjects: `ancplua.lightning.*`, max age: 24h, file storage
  - Storm: `ancplua-storm`, subjects: `ancplua.storm.*`, max age: 1h, memory storage

#### Non-Functional Requirements

**[NFR-1] Performance**
- EmitAsync MUST complete in <100ms (P95) for local NATS
- SubscribeAsync MUST buffer messages to prevent backpressure on NATS
- AggregateDiscoveries MUST handle 1000 discoveries in <500ms

**[NFR-2] Reliability**
- MUST retry transient NATS failures (connection loss, timeout) up to 3 times with exponential backoff
- MUST log errors to stderr (not stdout, per ServiceDefaults stdio discipline)
- MUST handle NATS server unavailability gracefully (log error, return empty results, not crash)

**[NFR-3] Observability**
- MUST emit OpenTelemetry traces for EmitAsync, SubscribeAsync, AggregateDiscoveries
- MUST record metrics: `whispermesh.emit.count`, `whispermesh.subscribe.count`, `whispermesh.aggregate.duration`
- MUST include `tier`, `topic`, `agent` as span attributes

**[NFR-4] Security**
- MUST validate all discovery DTOs against JSON schema (reject malformed messages)
- MUST sanitize paths in `CodeLocation` to prevent path traversal
- MUST NOT log sensitive data (credentials, tokens) in discovery messages

**[NFR-5] Testability**
- MUST provide in-memory fake `IWhisperMeshClient` for unit tests
- MUST use Testcontainers for integration tests (real NATS server)
- MUST achieve >80% code coverage for client, aggregator, and MCP tools

---

### Design

#### Components

**1. Ancplua.Mcp.WhisperMesh (existing, Phase 0)**
- Models: `WhisperTier`, `WhisperMetadata`, `WhisperMessage`
- Discoveries: `ArchitectureViolation`, `ImplementationIssue`, `CodeLocation`

**2. Ancplua.Mcp.WhisperMesh.Client (new, Phase 1)**
- `IWhisperMeshClient`: Abstraction for pub/sub operations
- `NatsWhisperMeshClient`: NATS JetStream implementation
- `WhisperEnvelope<TDiscovery>`: Wrapper for received messages with metadata

**3. Ancplua.Mcp.WhisperMesh.Aggregator (new, Phase 1)**
- `WhisperAggregator`: Service for deduplication and sorting
- `AggregatedWhisperReport`: DTO for aggregated results

**4. Ancplua.Mcp.WorkstationServer.Tools (update existing)**
- `WhisperAggregatorTools`: MCP tools for aggregation

**5. Ancplua.Mcp.ServiceDefaults (update existing)**
- DI registration for `IWhisperMeshClient`
- Configuration binding for NATS settings

#### Interfaces

##### IWhisperMeshClient

```csharp
namespace Ancplua.Mcp.WhisperMesh.Client;

/// <summary>
/// WhisperMesh pub/sub client abstraction.
/// Implementations: NatsWhisperMeshClient (production), InMemoryWhisperMeshClient (tests).
/// </summary>
public interface IWhisperMeshClient
{
    /// <summary>
    /// Emit a discovery to WhisperMesh.
    /// </summary>
    /// <typeparam name="TDiscovery">Discovery type (must have "type" property).</typeparam>
    /// <param name="tier">Tier (Lightning or Storm).</param>
    /// <param name="topic">Topic (e.g., "code-quality", "security.cve").</param>
    /// <param name="discovery">Discovery DTO.</param>
    /// <param name="metadata">Optional metadata for traceability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Message ID of emitted whisper.</returns>
    Task<string> EmitAsync<TDiscovery>(
        WhisperTier tier,
        string topic,
        TDiscovery discovery,
        WhisperMetadata? metadata = null,
        CancellationToken cancellationToken = default)
        where TDiscovery : class;

    /// <summary>
    /// Subscribe to discoveries from WhisperMesh.
    /// </summary>
    /// <typeparam name="TDiscovery">Discovery type to deserialize.</typeparam>
    /// <param name="tier">Tier to subscribe to (or null for all tiers).</param>
    /// <param name="topicPattern">Topic pattern (e.g., "code-quality", "security.*", "*").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of envelopes containing discoveries.</returns>
    IAsyncEnumerable<WhisperEnvelope<TDiscovery>> SubscribeAsync<TDiscovery>(
        WhisperTier? tier,
        string topicPattern,
        CancellationToken cancellationToken = default)
        where TDiscovery : class;
}

/// <summary>
/// Envelope for received whisper with metadata.
/// </summary>
public sealed record WhisperEnvelope<TDiscovery>
{
    public required string MessageId { get; init; }
    public required string Subject { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public required TDiscovery Discovery { get; init; }
    public required WhisperMetadata? Metadata { get; init; }
}
```

##### WhisperAggregator

```csharp
namespace Ancplua.Mcp.WhisperMesh.Aggregator;

/// <summary>
/// Aggregates and deduplicates WhisperMesh discoveries.
/// </summary>
public sealed class WhisperAggregator
{
    private readonly IWhisperMeshClient _client;

    public WhisperAggregator(IWhisperMeshClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Aggregate discoveries from WhisperMesh.
    /// </summary>
    /// <param name="request">Aggregation request (tiers, topics, time window).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated report with deduplicated discoveries.</returns>
    public Task<AggregatedWhisperReport> AggregateAsync(
        AggregateDiscoveriesRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for AggregateDiscoveries MCP tool.
/// </summary>
public sealed record AggregateDiscoveriesRequest
{
    /// <summary>
    /// Tiers to include (empty = all tiers).
    /// Valid values: "Lightning", "Storm".
    /// </summary>
    public IReadOnlyList<string> Tiers { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Topics to include (empty = all topics).
    /// Supports wildcards (e.g., "security.*").
    /// </summary>
    public IReadOnlyList<string> Topics { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Time window in minutes (null = no time filter).
    /// Only discoveries received in the last N minutes are included.
    /// </summary>
    public int? TimeWindowMinutes { get; init; }

    /// <summary>
    /// Max items to return (null = no limit).
    /// Applied after deduplication.
    /// </summary>
    public int? MaxItems { get; init; }
}

/// <summary>
/// Response for AggregateDiscoveries MCP tool.
/// </summary>
public sealed record AggregatedWhisperReport
{
    /// <summary>
    /// Deduplicated discoveries, sorted by tier (Lightning first) then severity (descending).
    /// Polymorphic: can contain ArchitectureViolation, ImplementationIssue, etc.
    /// </summary>
    public required IReadOnlyList<object> Discoveries { get; init; }

    /// <summary>
    /// Count of discoveries per tier.
    /// </summary>
    public required Dictionary<string, int> TierCounts { get; init; }

    /// <summary>
    /// Count of discoveries per agent.
    /// </summary>
    public required Dictionary<string, int> AgentBreakdown { get; init; }

    /// <summary>
    /// Total discoveries received before deduplication.
    /// </summary>
    public required int TotalReceived { get; init; }

    /// <summary>
    /// Total discoveries after deduplication.
    /// </summary>
    public required int TotalDeduplicated { get; init; }
}
```

##### MCP Tool: AggregateDiscoveries

```csharp
namespace Ancplua.Mcp.WorkstationServer.Tools;

public static class WhisperAggregatorTools
{
    /// <summary>
    /// Aggregate WhisperMesh discoveries from Dolphin Pod agents.
    /// </summary>
    /// <param name="request">Aggregation request (tiers, topics, time window).</param>
    /// <param name="aggregator">WhisperAggregator service (injected).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated report with deduplicated discoveries.</returns>
    [McpServerTool]
    [Description("Aggregate WhisperMesh discoveries from multiple agents")]
    public static Task<AggregatedWhisperReport> AggregateDiscoveries(
        [Description("Aggregation parameters (tiers, topics, time window)")] AggregateDiscoveriesRequest request,
        WhisperAggregator aggregator,
        CancellationToken cancellationToken = default)
    {
        return aggregator.AggregateAsync(request, cancellationToken);
    }
}
```

#### Data Models

All discovery types from Phase 0 are reused:
- `ArchitectureViolation`: ARCH agent findings
- `ImplementationIssue`: IMPL agent findings
- `CodeLocation`: File path, line range, code snippet

**New models**:
- `WhisperEnvelope<TDiscovery>`: Received message wrapper
- `AggregateDiscoveriesRequest`: MCP tool input
- `AggregatedWhisperReport`: MCP tool output

#### Behavior

**Emission Flow**:
1. Agent (ARCH, IMPL, Security) detects issue
2. Constructs discovery DTO (`ArchitectureViolation`, etc.)
3. Calls `client.EmitAsync(WhisperTier.Lightning, "architecture", discovery)`
4. Client serializes to JSON, publishes to `ancplua.lightning.architecture`
5. NATS JetStream stores message (24h TTL for Lightning, 1h for Storm)

**Subscription Flow**:
1. Aggregator calls `client.SubscribeAsync<ArchitectureViolation>(null, "*")`
2. Client subscribes to `ancplua.>` (all tiers, all topics)
3. NATS delivers messages from JetStream
4. Client deserializes JSON to `WhisperEnvelope<ArchitectureViolation>`
5. Aggregator deduplicates by `(location, type, agent)`
6. Aggregator sorts by tier (Lightning first) then severity (descending)

**Deduplication Key**:
```csharp
// Two discoveries are duplicates if:
// 1. Same CodeLocation (file path + line range)
// 2. Same discovery type (ArchitectureViolation vs ImplementationIssue)
// 3. Same agent (to preserve multi-agent consensus)
string dedupKey = $"{discovery.Location.FilePath}:{discovery.Location.StartLine}-{discovery.Location.EndLine}|{discovery.Type}|{discovery.Agent}";
```

#### Configuration

**appsettings.json** (WorkstationServer, HttpServer):
```json
{
  "WhisperMesh": {
    "NatsUrl": "nats://localhost:4222",
    "Streams": {
      "Lightning": {
        "Name": "ancplua-lightning",
        "Subjects": ["ancplua.lightning.*"],
        "MaxAge": "24h",
        "Storage": "file"
      },
      "Storm": {
        "Name": "ancplua-storm",
        "Subjects": ["ancplua.storm.*"],
        "MaxAge": "1h",
        "Storage": "memory"
      }
    }
  }
}
```

**Environment Variable Override**:
- `NATS_URL`: NATS server URL (overrides appsettings.json)

**docker-compose.yml** (local development):
```yaml
services:
  nats:
    image: nats:2.10-alpine
    ports:
      - "4222:4222"  # Client connections
      - "8222:8222"  # HTTP monitoring
    command:
      - "--jetstream"
      - "--store_dir=/data"
    volumes:
      - nats-data:/data

volumes:
  nats-data:
```

---

### Implementation Considerations

**1. DI Registration (ServiceDefaults)**

```csharp
public static class WhisperMeshExtensions
{
    public static IHostApplicationBuilder AddWhisperMesh(this IHostApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection("WhisperMesh");
        builder.Services.Configure<WhisperMeshOptions>(config);

        builder.Services.AddSingleton<IWhisperMeshClient, NatsWhisperMeshClient>();
        builder.Services.AddSingleton<WhisperAggregator>();

        return builder;
    }
}
```

**2. NATS Subject Naming Convention**

Pattern: `ancplua.{tier}.{topic}`

Examples:
- `ancplua.lightning.architecture` (ARCH agent critical findings)
- `ancplua.lightning.security.cve` (Security agent CVEs)
- `ancplua.storm.code-quality` (IMPL agent code smells)
- `ancplua.storm.performance` (Performance agent metrics)

**3. Deduplication Algorithm**

```csharp
var deduplicated = discoveries
    .GroupBy(d => GetDedupKey(d))
    .Select(g => g.First()) // Keep first occurrence
    .OrderBy(d => d.Tier == WhisperTier.Lightning ? 0 : 1) // Lightning first
    .ThenByDescending(d => GetSeverity(d)) // Highest severity first
    .ToList();

static string GetDedupKey(object discovery)
{
    return discovery switch
    {
        ArchitectureViolation av => $"{av.Location.FilePath}:{av.Location.StartLine}|ArchitectureViolation|{av.Agent}",
        ImplementationIssue ii => $"{ii.Location.FilePath}:{ii.Location.StartLine}|ImplementationIssue|{ii.Agent}",
        _ => Guid.NewGuid().ToString() // Unknown types are never duplicates
    };
}
```

**4. Resilience Strategy**

Use Polly policies (already in ServiceDefaults):
- Retry: 3 attempts with exponential backoff (500ms, 1s, 2s)
- Circuit breaker: Open after 5 consecutive failures, half-open after 30s
- Timeout: 10s for EmitAsync, 30s for SubscribeAsync initial connection

**5. Backward Compatibility**

Phase 0 contracts are NOT changed:
- `WhisperTier`, `WhisperMetadata`, `ArchitectureViolation`, `ImplementationIssue`, `CodeLocation` remain unchanged
- New code builds on top of existing foundation

---

### Testing

**Unit Tests (Ancplua.Mcp.WhisperMesh.Tests)**

1. `InMemoryWhisperMeshClient_EmitAndSubscribe_Success`
   - Emit 10 discoveries
   - Subscribe and verify all received
   - Verify envelope metadata correct

2. `WhisperAggregator_DeduplicatesByLocationAndType`
   - Emit 5 duplicate discoveries (same location, type, agent)
   - Emit 5 unique discoveries
   - Verify output has 5 items (duplicates removed)

3. `WhisperAggregator_SortsByTierThenSeverity`
   - Emit 3 Storm (severity 0.5, 0.7, 0.9)
   - Emit 3 Lightning (severity 0.5, 0.7, 0.9)
   - Verify order: Lightning 0.9, 0.7, 0.5, then Storm 0.9, 0.7, 0.5

4. `AggregateDiscoveries_TimeWindowFilter`
   - Emit 5 discoveries (timestamps: now, -1min, -3min, -5min, -10min)
   - Request timeWindowMinutes: 4
   - Verify output has 3 items (now, -1min, -3min)

**Integration Tests (Ancplua.Mcp.WorkstationServer.Tests)**

1. `NatsWhisperMeshClient_EmitAndSubscribe_WithRealNats`
   - Use Testcontainers to start NATS server
   - Create NatsWhisperMeshClient
   - Emit 10 ArchitectureViolations
   - Subscribe and verify all received

2. `MultiAgentScenario_ParallelEmission_Aggregation`
   - Start 3 emitters (ARCH, IMPL, Security)
   - Each emits 10 discoveries in parallel
   - Aggregator subscribes to all
   - Verify 30 discoveries received, deduplicated correctly

3. `NatsReconnection_TransientFailure_Retry`
   - Start NATS, emit 5 discoveries
   - Stop NATS container
   - Emit 5 more (should retry and fail)
   - Restart NATS
   - Emit 5 more (should succeed after reconnection)

**Coverage Target**: >80% for all new code (client, aggregator, MCP tools)

---

### Security Considerations

**1. Path Traversal Prevention**
- Validate `CodeLocation.FilePath` does not contain `..`, absolute paths outside workspace
- Sanitize before logging or returning to user

**2. Input Validation**
- Validate `tier` is valid enum value (Lightning, Storm)
- Validate `topic` matches pattern `^[a-z0-9._-]+$`
- Validate `severity` is 0.0-1.0
- Reject malformed JSON (log error, skip message)

**3. Sensitive Data**
- NEVER log credentials, API keys, tokens in discoveries
- Truncate code snippets in `CodeLocation` to 500 chars max
- Mask environment variables in error logs

**4. NATS Authentication**
- Phase 1: No auth (local dev only)
- Phase 2: Add JWT auth for production (separate ADR)

---

### Performance Considerations

**1. Throughput**
- Target: 100 discoveries/second per agent
- NATS JetStream can handle 10K+ msg/sec on commodity hardware

**2. Latency**
- EmitAsync: <100ms (P95) for local NATS
- SubscribeAsync: <50ms (P95) initial connection, <10ms per message

**3. Memory**
- Buffer max 10,000 discoveries in aggregator (LRU eviction)
- Storm discoveries: 1h TTL, auto-cleanup by NATS
- Lightning discoveries: 24h TTL, file-backed (no memory pressure)

**4. Backpressure**
- If subscriber falls behind, NATS JetStream buffers in stream
- If stream reaches max age, oldest messages auto-deleted
- Subscriber uses `await foreach` (built-in backpressure via IAsyncEnumerable)

---

## Alternatives Considered

**Alternative 1: In-Memory Queue (No NATS)**
- **Pros**: Simpler, no external dependency
- **Cons**: No persistence, no multi-process support, no subject routing
- **Decision**: Rejected. Cannot support Dolphin Pod multi-agent parallelism.

**Alternative 2: Redis Pub/Sub**
- **Pros**: Ubiquitous, easy to run
- **Cons**: No message replay, no subject hierarchy, ephemeral only
- **Decision**: Rejected per ADR-0107.

**Alternative 3: Direct HTTP Polling**
- Each agent exposes HTTP endpoint, orchestrator polls
- **Cons**: Tight coupling, no durability, polling inefficiency
- **Decision**: Rejected. Violates WhisperMesh "ambient intelligence" principle.

---

## Dependencies

**External Packages**:
- `NATS.Client.Core` (>= 2.0.0): NATS client library
- `NATS.Client.JetStream` (>= 2.0.0): JetStream API
- `Testcontainers.Nats` (>= 4.0.0): Integration tests
- `Polly` (>= 8.0.0): Resilience (already in ServiceDefaults)
- `OpenTelemetry.Api` (>= 1.9.0): Observability (already in ServiceDefaults)

**Internal Dependencies**:
- `Ancplua.Mcp.WhisperMesh` (Phase 0): Discovery types, models
- `Ancplua.Mcp.ServiceDefaults`: DI, OpenTelemetry, resilience

**Related Specs**:
- spec-whispermesh-protocol.md: Protocol v1.0 (1062 lines)
- spec-006-core-tools-library.md: Tool consolidation pattern

**Related ADRs**:
- ADR-0107: WhisperMesh Protocol Adoption (strategic decision)

---

## Timeline

- **2025-11-22**: Phase 0 completed (PR #39 merged)
- **2025-11-25**: spec-0107 created (this document)
- **2025-11-26 to 2025-12-16**: Phase 1 implementation (3 weeks)
  - Week 1: Client infrastructure, DI, config
  - Week 2: Aggregator, MCP tools, unit tests
  - Week 3: Integration tests, documentation, PR review

---

## References

- [ADR-0107: WhisperMesh Protocol Adoption](../decisions/ADR-0107-whispermesh-protocol-adoption.md)
- [spec-whispermesh-protocol.md](spec-whispermesh-protocol.md)
- [Issue #41: WhisperMesh Phase 1](https://github.com/ANcpLua/ancplua-mcp/issues/41)
- [NATS JetStream Documentation](https://docs.nats.io/nats-concepts/jetstream)
- [CLAUDE.md Section 2: Specs and Decisions](../../CLAUDE.md#2-the-law-specs-and-decisions)

---

## Appendix

### Example Dolphin Pod Workflow (Phase 1)

```
┌─────────────────────────────────────────────────────────────┐
│ Dolphin Pod PR Review: ancplua-mcp PR #44                  │
└─────────────────────────────────────────────────────────────┘

1. Three Claude instances start in parallel:
   - Claude-ARCH: Reviews against CLAUDE.md, ADRs, specs
   - Claude-IMPL: Reviews code quality, tests, style
   - Claude-Security: Reviews for CVEs, path traversal, injection

2. Each agent emits WhisperMesh discoveries:

   Claude-ARCH:
     EmitAsync(Lightning, "architecture", new ArchitectureViolation {
       Location = new CodeLocation { FilePath = "src/Tool.cs", StartLine = 42 },
       Rule = "CLAUDE.md#section-5.2",
       Finding = "Breaking tool contract change without ADR",
       Severity = 0.9,
       Agent = "Claude-ARCH"
     })

   Claude-IMPL:
     EmitAsync(Storm, "code-quality", new ImplementationIssue {
       Location = new CodeLocation { FilePath = "src/Tool.cs", StartLine = 42 },
       Category = "Complexity",
       Finding = "Method exceeds 15 lines",
       Severity = 0.6,
       Agent = "Claude-IMPL"
     })

3. Orchestrator aggregates:

   result = await AggregateDiscoveries(new AggregateDiscoveriesRequest {
     Tiers = ["Lightning", "Storm"],
     Topics = ["*"],
     TimeWindowMinutes = 5
   });

4. Orchestrator synthesizes final PR comment:

   "Found 12 issues (3 Lightning, 9 Storm):

   🔴 Critical (Lightning):
   - src/Tool.cs:42 - Breaking contract change without ADR
   - src/Another.cs:15 - Path traversal vulnerability
   - tests/Test.cs:8 - Missing test for new tool

   🟡 Ambient (Storm):
   - src/Tool.cs:42 - Method too complex (15+ lines)
   - ... (8 more)"
```

### NATS Subject Hierarchy

```
ancplua                           # Root namespace
├── lightning                     # Critical tier (24h TTL, file-backed)
│   ├── architecture              # ARCH agent findings
│   ├── security                  # Security agent findings
│   │   ├── cve                   # CVE discoveries
│   │   └── injection             # Injection vulnerabilities
│   └── build                     # Build failures
└── storm                         # Ambient tier (1h TTL, memory-backed)
    ├── code-quality              # IMPL agent findings
    ├── performance               # Performance metrics
    └── suggestions               # Refactoring suggestions
```

Wildcard subscriptions:
- `ancplua.>`: All tiers, all topics
- `ancplua.lightning.*`: All Lightning discoveries
- `ancplua.*.security.*`: Security from both tiers
- `ancplua.storm.code-quality`: Specific topic
