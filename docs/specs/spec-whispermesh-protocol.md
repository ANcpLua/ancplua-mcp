# WhisperMesh Protocol Specification v1.0

**Status**: RFC Draft
**Version**: 1.0.0
**Date**: 2025-11-22
**Authors**: ancplua-mcp maintainers

---

## 1. Introduction

### 1.1 Purpose

This document defines the **WhisperMesh Protocol**, an ambient agent communication protocol enabling autonomous software agents to share discoveries, context, and knowledge in real-time without blocking or centralized orchestration.

### 1.2 Scope

This specification covers:
- Message format and schema
- Transport protocol (NATS subjects)
- Agent identity and registration
- Discovery types and semantics
- Delivery guarantees per tier
- Observability requirements
- Security and validation

### 1.3 Design Goals

1. **Non-blocking**: Agents never block waiting for responses
2. **Tiered urgency**: Critical discoveries (Lightning) vs. ambient knowledge (Storm)
3. **Interest-based routing**: Agents subscribe only to relevant topics
4. **Structured data**: Machine-readable, actionable discoveries
5. **Observable**: Full OpenTelemetry instrumentation
6. **Extensible**: Forward-compatible with future versions

### 1.4 Terminology

- **Whisper**: A message broadcast by an agent containing a discovery
- **Agent**: An autonomous software entity (LLM, IDE plugin, MCP server, CLI tool)
- **Discovery**: Structured data representing a finding (metric, vulnerability, suggestion)
- **Tier**: Urgency classification (`lightning` or `storm`)
- **Topic**: Hierarchical category for routing (e.g., `csharp`, `security.cve`)
- **Interest**: Set of topics an agent subscribes to
- **Subject**: NATS pub/sub routing key (e.g., `ancplua.storm.csharp`)

---

## 2. Message Format

### 2.1 WhisperMessage Schema

All whispers MUST conform to the following JSON schema:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["agent", "tier", "topic", "severity", "timestamp", "messageId"],
  "properties": {
    "messageId": {
      "type": "string",
      "format": "uuid",
      "description": "Unique identifier for this whisper (UUIDv4)"
    },
    "agent": {
      "type": "string",
      "pattern": "^[a-zA-Z0-9_-]+$",
      "maxLength": 64,
      "description": "Agent identity (alphanumeric, dash, underscore)"
    },
    "tier": {
      "type": "string",
      "enum": ["lightning", "storm"],
      "description": "Urgency tier: lightning (critical) or storm (ambient)"
    },
    "topic": {
      "type": "string",
      "pattern": "^[a-z0-9._-]+$",
      "maxLength": 128,
      "description": "Dot-separated category (e.g., code-quality, security.cve)"
    },
    "severity": {
      "type": "number",
      "minimum": 0.0,
      "maximum": 1.0,
      "description": "Normalized urgency score (0.0 = informational, 1.0 = critical)"
    },
    "message": {
      "type": "string",
      "maxLength": 1024,
      "description": "Optional human-readable summary"
    },
    "discovery": {
      "type": "object",
      "description": "Structured, type-specific discovery data",
      "required": ["type"],
      "properties": {
        "type": {
          "type": "string",
          "description": "Discovery type (e.g., CyclomaticComplexity, SecurityVulnerability)"
        }
      },
      "additionalProperties": true
    },
    "metadata": {
      "type": "object",
      "description": "Context metadata for traceability",
      "properties": {
        "project": { "type": "string" },
        "repository": { "type": "string" },
        "commit": { "type": "string", "pattern": "^[a-f0-9]{40}$" },
        "branch": { "type": "string" },
        "toolVersion": { "type": "string" },
        "language": { "type": "string" },
        "framework": { "type": "string" },
        "schemaVersion": { "type": "string", "pattern": "^\\d+\\.\\d+\\.\\d+$" }
      },
      "additionalProperties": true
    },
    "timestamp": {
      "type": "string",
      "format": "date-time",
      "description": "ISO 8601 UTC timestamp (e.g., 2025-11-22T16:41:00Z)"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid",
      "description": "Optional: links related whispers (e.g., for distributed tracing)"
    },
    "expiresAt": {
      "type": "string",
      "format": "date-time",
      "description": "Optional: when this whisper becomes stale"
    }
  }
}
```

### 2.2 Example WhisperMessage

```json
{
  "messageId": "550e8400-e29b-41d4-a716-446655440000",
  "agent": "RoslynMetricsServer",
  "tier": "storm",
  "topic": "code-quality",
  "severity": 0.7,
  "message": "High cyclomatic complexity detected in ProcessData()",
  "discovery": {
    "type": "CyclomaticComplexity",
    "location": {
      "file": "src/Core/Processor.cs",
      "line": 142,
      "column": 5,
      "symbol": "ProcessData()"
    },
    "metric": "cyclomatic_complexity",
    "value": 42,
    "threshold": 15,
    "recommendation": "Split into smaller methods using Extract Method refactoring"
  },
  "metadata": {
    "project": "ancplua-mcp",
    "repository": "https://github.com/ANcpLua/ancplua-mcp",
    "commit": "abc123def456789012345678901234567890abcd",
    "branch": "main",
    "toolVersion": "1.0.0",
    "language": "csharp",
    "framework": "net10.0",
    "schemaVersion": "1.0.0"
  },
  "timestamp": "2025-11-22T16:41:00Z",
  "correlationId": "parent-trace-id-if-any"
}
```

### 2.3 Field Requirements

#### 2.3.1 Required Fields

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `messageId` | UUID | UUIDv4 | Unique identifier for deduplication |
| `agent` | string | `^[a-zA-Z0-9_-]{1,64}$` | Agent identity (no spaces, dots) |
| `tier` | enum | `lightning` \| `storm` | Urgency classification |
| `topic` | string | `^[a-z0-9._-]{1,128}$` | Routing category |
| `severity` | float | 0.0 - 1.0 | Normalized urgency |
| `timestamp` | ISO8601 | UTC format | When whisper was created |

#### 2.3.2 Optional Fields

| Field | Type | Description |
|-------|------|-------------|
| `message` | string | Human-readable summary (max 1024 chars) |
| `discovery` | object | Structured discovery data (type-specific) |
| `metadata` | object | Traceability context |
| `correlationId` | UUID | For distributed tracing |
| `expiresAt` | ISO8601 | When whisper becomes stale |

#### 2.3.3 Discovery Field

The `discovery` object is **OPTIONAL** but **RECOMMENDED**. When present:
- MUST include `type` field (string, PascalCase)
- MAY include any type-specific fields
- SHOULD follow known discovery type schemas (see §4)

---

## 3. Transport Protocol (NATS)

### 3.1 Subject Hierarchy

WhisperMesh uses **NATS** as the transport layer with hierarchical subjects:

```
ancplua.<tier>.<topic>[.<subtopic>]
```

**Examples**:
```
ancplua.lightning.security
ancplua.lightning.baseline
ancplua.storm.dotnet
ancplua.storm.csharp
ancplua.storm.code-quality
ancplua.storm.dependencies.nuget
```

### 3.2 Subject Naming Rules

1. **Prefix**: All subjects MUST start with `ancplua.`
2. **Tier**: Second segment MUST be `lightning` or `storm`
3. **Topic**: Third+ segments MUST match `[a-z0-9._-]+`
4. **Lowercase**: All subject segments MUST be lowercase
5. **Depth**: Maximum 6 levels (e.g., `ancplua.storm.security.cve.critical.dotnet`)

### 3.3 Wildcard Subscriptions

Agents MAY use NATS wildcards:

| Pattern | Matches | Example |
|---------|---------|---------|
| `*` | Single level | `ancplua.storm.*` (all storm topics) |
| `>` | Multiple levels | `ancplua.lightning.>` (all lightning, any depth) |

**Examples**:
```
ancplua.storm.*                  # All storm whispers (one level deep)
ancplua.lightning.>              # All lightning whispers (any depth)
ancplua.*.security               # Security whispers (any tier)
ancplua.storm.dependencies.>     # All dependency whispers
```

### 3.4 JetStream Configuration

#### 3.4.1 Lightning Stream (Durable)

```json
{
  "name": "LIGHTNING",
  "subjects": ["ancplua.lightning.>"],
  "retention": "limits",
  "max_age": 86400000000000,
  "max_msgs": 100000,
  "max_msgs_per_subject": 1000,
  "storage": "file",
  "num_replicas": 1,
  "discard": "old"
}
```

**Characteristics**:
- **Persistence**: File-backed, survives restarts
- **TTL**: 24 hours
- **Replay**: Consumers can replay missed messages
- **Replication**: 1 replica (3 in production clusters)

#### 3.4.2 Storm Stream (Ephemeral)

```json
{
  "name": "STORM",
  "subjects": ["ancplua.storm.>"],
  "retention": "interest",
  "max_age": 3600000000000,
  "storage": "memory",
  "num_replicas": 1,
  "discard": "old"
}
```

**Characteristics**:
- **Persistence**: Memory-only, lost on restart
- **TTL**: 1 hour
- **Replay**: Limited to active consumers
- **Performance**: Faster than Lightning

### 3.5 Publishing

```plaintext
Subject: ancplua.<tier>.<topic>
Payload: JSON-encoded WhisperMessage
Headers:
  - Nats-Msg-Id: <messageId>  (for deduplication)
  - Content-Type: application/json
  - Schema-Version: 1.0.0
```

**Example** (pseudocode):
```csharp
await natsConnection.PublishAsync(
    subject: "ancplua.storm.code-quality",
    data: JsonSerializer.SerializeToUtf8Bytes(whisperMessage),
    headers: new Dictionary<string, string> {
        { "Nats-Msg-Id", whisperMessage.MessageId },
        { "Content-Type", "application/json" },
        { "Schema-Version", "1.0.0" }
    }
);
```

### 3.6 Subscribing

```plaintext
Subscribe to: ancplua.<tier>.<topic> (or wildcard)
Consumer: Durable (Lightning) or Ephemeral (Storm)
Ack: Manual acknowledgment for Lightning
```

**Example** (pseudocode):
```csharp
await subscription.SubscribeAsync(
    subject: "ancplua.storm.csharp",
    handler: async (msg) => {
        var whisper = JsonSerializer.Deserialize<WhisperMessage>(msg.Data);
        await ProcessWhisperAsync(whisper);
    }
);
```

---

## 4. Discovery Types

### 4.1 Standard Discovery Types

WhisperMesh defines standard discovery types for interoperability. Agents SHOULD use these types when applicable.

#### 4.1.1 CyclomaticComplexity

```json
{
  "type": "CyclomaticComplexity",
  "location": {
    "file": "string",
    "line": "integer",
    "column": "integer (optional)",
    "symbol": "string"
  },
  "metric": "cyclomatic_complexity",
  "value": "number",
  "threshold": "number",
  "recommendation": "string (optional)"
}
```

**Topics**: `code-quality`, `csharp`, `java`, etc.
**Tier**: Usually `storm`

---

#### 4.1.2 SecurityVulnerability

```json
{
  "type": "SecurityVulnerability",
  "cve": "string (e.g., CVE-2025-1234)",
  "package": "string",
  "version": "string",
  "severity": "Critical | High | Medium | Low",
  "cvssScore": "number (0.0-10.0)",
  "exploitability": "number (0.0-1.0)",
  "patch": "string (version with fix)",
  "references": ["string (URLs)"]
}
```

**Topics**: `security`, `security.cve`, `dependencies.security`
**Tier**: Usually `lightning`

---

#### 4.1.3 FrameworkBaseline

```json
{
  "type": "FrameworkBaseline",
  "framework": "string (e.g., net10.0)",
  "from": "string (previous version)",
  "to": "string (new version)",
  "breaking_changes": ["string"],
  "new_features": ["string"],
  "deprecated": ["string"],
  "migration_guide": "string (URL)"
}
```

**Topics**: `baseline`, `dotnet`, `frameworks`
**Tier**: Usually `lightning`

---

#### 4.1.4 DependencyUpdate

```json
{
  "type": "DependencyUpdate",
  "package": "string",
  "ecosystem": "nuget | npm | pypi | maven",
  "current": "string (semver)",
  "latest": "string (semver)",
  "changeType": "major | minor | patch",
  "breaking": "boolean",
  "securityFix": "boolean",
  "releaseNotes": "string (URL)"
}
```

**Topics**: `dependencies`, `dependencies.nuget`, `dependencies.npm`
**Tier**: `lightning` (if security/breaking), `storm` (otherwise)

---

#### 4.1.5 CodeSmell

```json
{
  "type": "CodeSmell",
  "smell": "LongMethod | GodClass | DataClumps | ...",
  "location": {
    "file": "string",
    "line": "integer",
    "symbol": "string"
  },
  "metrics": {
    "lines": "integer",
    "parameters": "integer",
    "dependencies": "integer"
  },
  "recommendation": "string"
}
```

**Topics**: `code-quality`, `refactoring`
**Tier**: Usually `storm`

---

#### 4.1.6 TestCoverageGap

```json
{
  "type": "TestCoverageGap",
  "file": "string",
  "coverage": "number (0.0-1.0)",
  "threshold": "number (0.0-1.0)",
  "uncovered_lines": ["integer"],
  "suggestion": "string"
}
```

**Topics**: `testing`, `coverage`
**Tier**: Usually `storm`

---

#### 4.1.7 BuildFailure

```json
{
  "type": "BuildFailure",
  "project": "string",
  "errorCode": "string",
  "message": "string",
  "file": "string (optional)",
  "line": "integer (optional)",
  "logs": "string (truncated)"
}
```

**Topics**: `build`, `ci`
**Tier**: Usually `lightning`

---

### 4.2 Custom Discovery Types

Agents MAY define custom discovery types. When doing so:

1. **Naming**: Use PascalCase (e.g., `MyCustomDiscovery`)
2. **Versioning**: Include `schemaVersion` in metadata
3. **Documentation**: Publish schema in agent documentation
4. **Namespace**: Prefix with vendor (e.g., `Ancplua.RoslynComplexity`)

**Example**:
```json
{
  "type": "Ancplua.RoslynComplexity",
  "customField1": "...",
  "customField2": "..."
}
```

Unknown discovery types MUST be ignored by agents (forward compatibility).

---

## 5. Agent Identity

### 5.1 Agent Naming

Agent names MUST:
- Be unique within the WhisperMesh
- Match regex: `^[a-zA-Z0-9_-]{1,64}$`
- Be descriptive (e.g., `RoslynMetricsServer`, not `agent1`)
- NOT contain dots, spaces, or special characters (except dash, underscore)

**Valid**: `ClaudeCode`, `Rider-Agent`, `Gemini_CLI`
**Invalid**: `claude.code`, `Rider Agent`, `gemini@cli`

### 5.2 Agent Registration

Agents SHOULD publish a **heartbeat whisper** on startup:

```json
{
  "messageId": "...",
  "agent": "RoslynMetricsServer",
  "tier": "storm",
  "topic": "agent.online",
  "severity": 0.1,
  "message": "Agent online",
  "discovery": {
    "type": "AgentOnline",
    "version": "1.0.0",
    "capabilities": ["code-quality", "csharp-analysis"],
    "interests": ["storm.csharp", "lightning.baseline"]
  },
  "timestamp": "2025-11-22T16:41:00Z"
}
```

Subject: `ancplua.storm.agent.online`

### 5.3 Agent Deregistration

Agents SHOULD publish a **shutdown whisper** on graceful shutdown:

```json
{
  "messageId": "...",
  "agent": "RoslynMetricsServer",
  "tier": "storm",
  "topic": "agent.offline",
  "severity": 0.1,
  "message": "Agent shutting down",
  "discovery": {
    "type": "AgentOffline",
    "reason": "graceful-shutdown"
  },
  "timestamp": "2025-11-22T16:50:00Z"
}
```

Subject: `ancplua.storm.agent.offline`

---

## 6. Interest Declaration

### 6.1 Interest Format

Agents declare interests as **topic patterns**:

```json
{
  "agent": "ClaudeCode",
  "interests": [
    "storm.dotnet",
    "storm.csharp",
    "lightning.baseline",
    "lightning.security"
  ]
}
```

### 6.2 Interest Matching

Interests map to NATS subscriptions:

| Interest | NATS Subject |
|----------|--------------|
| `storm.dotnet` | `ancplua.storm.dotnet` |
| `lightning.*` | `ancplua.lightning.*` |
| `*.security` | `ancplua.*.security` |
| `storm.dependencies.>` | `ancplua.storm.dependencies.>` |

### 6.3 Dynamic Interests

Agents MAY change interests at runtime by:
1. Unsubscribing from old subjects
2. Subscribing to new subjects
3. Publishing `AgentInterestUpdate` whisper (optional, for observability)

---

## 7. Severity Levels

### 7.1 Severity Scale

Severity is a **normalized float** from 0.0 to 1.0:

| Range | Level | Description | Typical Use |
|-------|-------|-------------|-------------|
| 0.0 - 0.2 | Informational | No action needed | Metrics, status updates |
| 0.3 - 0.5 | Low | Minor issues | Code smells, suggestions |
| 0.6 - 0.7 | Medium | Notable findings | Complexity warnings, test gaps |
| 0.8 - 0.9 | High | Significant issues | Security concerns, deprecated APIs |
| 1.0 | Critical | Immediate action | CVEs, build failures, breaking changes |

### 7.2 Severity Guidelines

- **Lightning tier**: Usually ≥ 0.6
- **Storm tier**: Usually < 0.6
- Agents MAY filter by severity threshold
- Severity is **subjective** to the emitting agent

---

## 8. Time-To-Live (TTL)

### 8.1 Default TTL by Tier

| Tier | Default TTL | Rationale |
|------|-------------|-----------|
| Lightning | 24 hours | Critical info should persist |
| Storm | 1 hour | Ambient knowledge is transient |

### 8.2 Custom TTL

Agents MAY set `expiresAt` for custom TTL:

```json
{
  "timestamp": "2025-11-22T16:00:00Z",
  "expiresAt": "2025-11-22T17:00:00Z"
}
```

Consumers SHOULD ignore whispers where `expiresAt < now()`.

---

## 9. Observability

### 9.1 OpenTelemetry Integration

All WhisperMesh operations MUST be instrumented with OpenTelemetry:

#### 9.1.1 Traces

**Emit Span**:
```plaintext
Span: whispermesh.emit
Attributes:
  - whispermesh.agent: "RoslynMetricsServer"
  - whispermesh.tier: "storm"
  - whispermesh.topic: "code-quality"
  - whispermesh.severity: 0.7
  - whispermesh.message_id: "550e8400-..."
```

**Consume Span**:
```plaintext
Span: whispermesh.consume
Attributes:
  - whispermesh.agent: "ClaudeCode"
  - whispermesh.source_agent: "RoslynMetricsServer"
  - whispermesh.tier: "storm"
  - whispermesh.topic: "code-quality"
  - whispermesh.latency_ms: 42
```

#### 9.1.2 Metrics

**Counters**:
- `whispermesh.whispers.emitted{tier, topic, agent}`
- `whispermesh.whispers.consumed{tier, topic, agent}`
- `whispermesh.whispers.dropped{reason, tier, agent}`

**Histograms**:
- `whispermesh.emit.duration_ms{tier, topic}`
- `whispermesh.consume.latency_ms{tier, topic}`

**Gauges**:
- `whispermesh.agents.online{agent}`
- `whispermesh.subscriptions.count{agent}`

#### 9.1.3 Logs

Whisper events SHOULD be logged at appropriate levels:

```plaintext
INFO:  Whisper emitted [agent=RoslynMetricsServer, topic=code-quality, severity=0.7]
DEBUG: Whisper consumed [source=RoslynMetricsServer, latency=42ms]
WARN:  Whisper dropped [reason=invalid-schema, agent=BadAgent]
ERROR: Whisper transport failure [nats_error=connection-timeout]
```

### 9.2 Correlation IDs

For distributed tracing, whispers SHOULD include `correlationId`:

```json
{
  "correlationId": "parent-trace-id",
  "metadata": {
    "traceId": "opentelemetry-trace-id",
    "spanId": "opentelemetry-span-id"
  }
}
```

---

## 10. Security

### 10.1 Authentication

**Local Development**:
- No authentication (localhost-only NATS)

**Production**:
- NATS JWT tokens
- One token per agent
- Token includes agent name claim

### 10.2 Authorization

**Subject Permissions**:
```plaintext
Agent: RoslynMetricsServer
Can publish to:
  - ancplua.storm.code-quality
  - ancplua.storm.csharp
Can subscribe to:
  - ancplua.lightning.>
  - ancplua.storm.>
```

Agents MUST NOT publish to subjects they don't own.

### 10.3 Data Sanitization

Whispers MUST NOT contain:
- Secrets (API keys, passwords, tokens)
- PII (emails, names, addresses)
- Absolute file paths (use relative paths)
- Proprietary code snippets (unless in private mesh)

### 10.4 Rate Limiting

**Per-Agent Limits**:
- Lightning: 10 whispers/minute
- Storm: 100 whispers/minute

Exceeding limits results in dropped whispers (logged as `whispermesh.whispers.dropped{reason=rate-limit}`).

---

## 11. Validation

### 11.1 Schema Validation

Consumers SHOULD validate whispers against the JSON schema (§2.1).

Invalid whispers MUST be:
- Logged as errors
- Dropped (not processed)
- Counted in `whispermesh.whispers.dropped{reason=invalid-schema}`

### 11.2 Required Field Validation

Whispers lacking required fields (§2.3.1) MUST be rejected.

### 11.3 Topic Validation

Topics MUST match `^[a-z0-9._-]{1,128}$`. Invalid topics MUST be rejected.

### 11.4 Timestamp Validation

Timestamps MUST:
- Be ISO 8601 UTC format
- Not be in the future (> 5 minutes drift allowed)
- Not be too old (> 7 days)

---

## 12. Error Handling

### 12.1 Transport Failures

If NATS connection fails:
- Log error
- Retry with exponential backoff (max 60s)
- Emit metric: `whispermesh.transport.failures{reason}`

### 12.2 Invalid Whispers

If whisper is malformed:
- Log warning with details
- Drop whisper
- Emit metric: `whispermesh.whispers.dropped{reason=invalid}`

### 12.3 Unknown Discovery Types

If `discovery.type` is unknown:
- Process whisper anyway (ignore discovery payload)
- Log debug message
- Continue normal operation (forward compatibility)

---

## 13. Versioning

### 13.1 Protocol Versioning

This specification is version **1.0.0** (semantic versioning).

**Version header**:
```plaintext
Schema-Version: 1.0.0
```

### 13.2 Backward Compatibility

**Minor/Patch changes** (1.0.x, 1.x.0) MUST be backward compatible:
- New optional fields allowed
- New discovery types allowed
- New topics allowed

**Major changes** (2.0.0) MAY break compatibility:
- Remove required fields
- Change field semantics
- Change subject hierarchy

### 13.3 Forward Compatibility

Consumers MUST ignore unknown fields (for forward compatibility with 1.x versions).

---

## 14. Extension Points

### 14.1 Custom Topics

Projects MAY define custom topic namespaces:

```plaintext
ancplua.storm.myproject.*
ancplua.lightning.custom.>
```

### 14.2 Custom Discovery Types

Agents MAY define vendor-specific discovery types (see §4.2).

### 14.3 Custom Metadata

The `metadata` object supports arbitrary fields for project-specific needs.

---

## 15. Implementation Guidelines

### 15.1 Client Libraries

WhisperMesh client libraries SHOULD provide:

```csharp
interface IWhisperMeshClient {
    Task EmitAsync(WhisperMessage whisper, CancellationToken ct);
    Task SubscribeAsync(string[] interests, Func<WhisperMessage, Task> handler, CancellationToken ct);
    Task UnsubscribeAsync(string[] interests, CancellationToken ct);
}
```

### 15.2 Configuration

Clients SHOULD support configuration via:
- Environment variables (`WHISPERMESH_NATS_URL`, `WHISPERMESH_AGENT_NAME`)
- appsettings.json
- Code-based options

### 15.3 Health Checks

WhisperMesh clients SHOULD expose health endpoints:

```plaintext
GET /health/whispermesh
Response:
{
  "status": "healthy",
  "nats_connected": true,
  "subscriptions": 5,
  "whispers_emitted": 1234,
  "whispers_consumed": 567
}
```

---

## 16. Testing

### 16.1 Contract Tests

Implementations SHOULD include contract tests verifying:
- Message schema compliance
- Subject naming rules
- Timestamp format
- Severity ranges

### 16.2 Integration Tests

Implementations SHOULD test:
- Emit → Consume flow
- Wildcard subscriptions
- TTL expiration
- Rate limiting

---

## 17. Future Considerations

### 17.1 Planned for v1.1

- **Request/Reply**: Optional acknowledgment for Lightning whispers
- **Whisper Analytics**: Aggregated stats (whispers/agent, topics trending)
- **Conflict Detection**: Multiple agents reporting contradictory discoveries

### 17.2 Planned for v2.0

- **Knowledge Graph**: RDF/SPARQL query support
- **Multi-Tenant**: Namespace isolation for multiple projects
- **Federated Mesh**: Cross-organization whisper sharing

---

## 18. References

- [NATS Documentation](https://docs.nats.io/)
- [NATS JetStream](https://docs.nats.io/nats-concepts/jetstream)
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/)
- [JSON Schema Draft 7](http://json-schema.org/draft-07/schema)
- [Semantic Versioning](https://semver.org/)

---

## Appendix A: Subject Examples

```plaintext
# Core subjects
ancplua.lightning.security
ancplua.lightning.baseline
ancplua.lightning.build
ancplua.storm.dotnet
ancplua.storm.csharp
ancplua.storm.code-quality

# Hierarchical examples
ancplua.storm.dependencies.nuget
ancplua.storm.security.cve
ancplua.storm.testing.coverage
ancplua.lightning.security.critical

# Agent lifecycle
ancplua.storm.agent.online
ancplua.storm.agent.offline
ancplua.storm.agent.interest-update
```

---

## Appendix B: Complete Example

**Scenario**: RoslynMetricsServer detects high complexity and emits a whisper.

**Whisper**:
```json
{
  "messageId": "550e8400-e29b-41d4-a716-446655440000",
  "agent": "RoslynMetricsServer",
  "tier": "storm",
  "topic": "code-quality",
  "severity": 0.7,
  "message": "High cyclomatic complexity detected in ProcessData()",
  "discovery": {
    "type": "CyclomaticComplexity",
    "location": {
      "file": "src/Core/Processor.cs",
      "line": 142,
      "column": 5,
      "symbol": "ProcessData()"
    },
    "metric": "cyclomatic_complexity",
    "value": 42,
    "threshold": 15,
    "recommendation": "Split into smaller methods using Extract Method refactoring"
  },
  "metadata": {
    "project": "ancplua-mcp",
    "repository": "https://github.com/ANcpLua/ancplua-mcp",
    "commit": "abc123def456789012345678901234567890abcd",
    "branch": "main",
    "toolVersion": "1.0.0",
    "language": "csharp",
    "framework": "net10.0",
    "schemaVersion": "1.0.0"
  },
  "timestamp": "2025-11-22T16:41:00Z"
}
```

**NATS Subject**: `ancplua.storm.code-quality`

**Subscribers**: Agents with interests matching:
- `storm.code-quality`
- `storm.*`
- `*.code-quality`

**OpenTelemetry Trace**:
```plaintext
Span: whispermesh.emit
  whispermesh.agent: RoslynMetricsServer
  whispermesh.tier: storm
  whispermesh.topic: code-quality
  whispermesh.severity: 0.7
  whispermesh.message_id: 550e8400-e29b-41d4-a716-446655440000
```

---

## Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-11-22 | Initial specification |

---

**End of WhisperMesh Protocol Specification v1.0**
