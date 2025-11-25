# ADR-0107: WhisperMesh Protocol Adoption

## Architecture Decision Record

### Title
Adopt WhisperMesh Protocol for Multi-Agent Ambient Intelligence

### Status
Accepted

### Date
2025-11-25

## Context

MCP (Model Context Protocol) provides excellent request/response tooling, but has fundamental limitations for multi-agent collaboration:

**MCP Limitations:**
- **Synchronous only**: Client calls tool, waits for response
- **No agent-to-agent communication**: Agents operate in isolation
- **No ambient intelligence**: Discoveries require explicit polling
- **No persistence**: Tool results are ephemeral, lost on session end
- **Single-client focus**: Cannot coordinate multiple AI agents (Claude, Gemini, Jules, CodeRabbit)

**Concrete Use Cases MCP Cannot Solve:**

1. **Dolphin Pod Evolution**: Current PR review simulates multi-agent (one Claude instance plays ARCH + IMPL roles sequentially). True multi-agent requires parallel execution with shared context.

2. **Cross-Repository Context**: RoslynMetrics finds vulnerability in ancplua-api → need to notify ancplua-web, ancplua-claude-plugins. MCP is repo-scoped.

3. **Background Analysis**: Continuous code quality monitoring floods MCP with requests. Need ambient whispers, not polling.

4. **Historical Debugging**: "Show me all security findings from yesterday's failed PRs." MCP has no time-series storage.

**Requirements:**
- Pub/sub messaging for agent-to-agent broadcast
- Durable streams for critical discoveries (24h replay)
- Ephemeral streams for ambient intelligence (1h TTL)
- OpenTelemetry-first observability
- Language-agnostic protocol (not C#-only)
- Subject-based routing for fine-grained subscriptions

## Decision

We will adopt the **WhisperMesh Protocol** (spec-whispermesh-protocol.md) using **NATS JetStream** as the transport layer for ambient multi-agent intelligence.

WhisperMesh will operate **alongside MCP**, not replace it:
- **MCP**: Request/response control plane ("do this task")
- **WhisperMesh**: Pub/sub data plane ("this happened")

## Rationale

**1. Dolphin Pod Strategic Alignment**

WhisperMesh transforms our Dolphin Pod PR review from simulated multi-agent to true parallel execution:

- **Current**: One Claude instance simulates ARCH + IMPL sequentially
- **Future**: Dedicated ARCH agent (Claude) + IMPL agent (Gemini/Jules) + Security agent (CodeRabbit) run in parallel, emit whispers, orchestrator synthesizes

**2. Protocol Quality**

The spec-whispermesh-protocol.md (1062 lines) is production-grade:
- Dual-tier system (Lightning/Storm) solves signal-to-noise problem
- Subject hierarchy enables wildcard subscriptions
- Forward-compatible (unknown discovery types ignored)
- OpenTelemetry baked in, not bolted on

**3. NATS JetStream Technical Superiority**

NATS provides capabilities Redis Pub/Sub and RabbitMQ cannot match:
- **Subject-based routing**: `ancplua.storm.csharp`, `ancplua.lightning.security.*`
- **JetStream replay**: Consumer can replay missed messages (Lightning: 24h, Storm: 1h)
- **Exactly-once delivery**: Message deduplication via `Nats-Msg-Id` header
- **Battle-tested**: Used by CloudFlare, Comcast, GE, Siemens

**4. Strategic Moat**

No other MCP server ecosystem has agent-to-agent ambient intelligence. This is a 10x differentiator.

## Consequences

### Positive Consequences
- True multi-agent PR reviews (ARCH + IMPL + Security run in parallel)
- Cross-repository context sharing (discoveries propagate across repos)
- Historical debugging ("show all complexity whispers from yesterday")
- IDE plugin bridge (Rider, VS Code subscribe to same NATS topics)
- Foundation for future agents (Gemini, Jules, CodeRabbit integration)

### Negative Consequences
- **8-12 week implementation investment** (currently 5% complete)
- **NATS infrastructure requirement** (docker-compose for local dev, NATS server for prod)
- **Learning curve** for contributors (NATS concepts: subjects, JetStream, consumers)
- **Operational complexity** (one more service to monitor/deploy)

### Neutral Consequences
- MCP servers can function without WhisperMesh (it's additive, not required)
- Spec reserves 0100-0199 range for ancplua-mcp (WhisperMesh is 0107)

## Alternatives Considered

### Alternative 1: Redis Pub/Sub
**Pros:**
- Ubiquitous, easy to run locally
- Simple mental model

**Cons:**
- No subject hierarchy (must subscribe to exact keys, no wildcards)
- No message replay (ephemeral only)
- No exactly-once delivery
- Pub/Sub is fire-and-forget (no durability)

**Decision:** Rejected. Cannot meet "show me yesterday's findings" requirement.

### Alternative 2: RabbitMQ
**Pros:**
- Topic exchanges support routing patterns
- Durable queues available

**Cons:**
- Heavier operational footprint than NATS
- No subject hierarchy (uses routing keys, not subject trees)
- Complex ACL model vs. NATS simple subject permissions
- Slower for small messages (<64KB, typical for discoveries)

**Decision:** Rejected. Overkill for our message sizes, harder to operate.

### Alternative 3: Apache Kafka
**Pros:**
- Industry-standard event streaming
- Excellent replay semantics

**Cons:**
- Massive operational complexity (ZooKeeper/KRaft, multi-broker clusters)
- Optimized for high-throughput large messages (>1MB)
- Overkill for <64KB discoveries
- Topic-based, not subject-based (less flexible routing)

**Decision:** Rejected. Kafka is for event streams, not lightweight agent coordination.

### Alternative 4: Extend MCP with Pub/Sub
**Pros:**
- No new protocol to learn
- Reuse existing MCP infrastructure

**Cons:**
- MCP spec is request/response by design (would violate spec philosophy)
- No durable streams in MCP
- No time-series replay
- Would fragment MCP ecosystem (non-standard extension)

**Decision:** Rejected. Better to complement MCP than fork it.

## Implementation Notes

**Phased Rollout (Fail-Fast Approach):**

1. **Phase 1 (Weeks 1-3)**: Minimum viable proof-of-concept
   - Fix build (WhisperTier, WhisperMetadata)
   - Implement WhisperMeshClient (EmitAsync, SubscribeAsync)
   - Add EmitWhisper/SubscribeToWhispers MCP tools to WorkstationServer
   - Integration test with real NATS container
   - **Checkpoint**: If Phase 1 doesn't prove value, kill project

2. **Phase 2 (Weeks 4-6)**: Production hardening
   - OpenTelemetry spans, metrics, logs
   - Rate limiting (10/min Lightning, 100/min Storm)
   - Security (JWT auth, data sanitization)
   - Remaining discovery types

3. **Phase 3 (Weeks 7-8)**: Dolphin Pod integration
   - ARCH/IMPL agents emit whispers
   - Orchestrator synthesizes from whisper stream

**Breaking Changes:**
- None. WhisperMesh is additive. MCP servers work without it.

**Required Resources:**
- NATS server (docker-compose for local, managed service or self-hosted for prod)
- ~8-12 weeks engineering time

## Related Decisions

- [spec-whispermesh-protocol.md](../specs/spec-whispermesh-protocol.md) - Protocol specification (v1.0)

## References

- [NATS Documentation](https://docs.nats.io/)
- [NATS JetStream](https://docs.nats.io/nats-concepts/jetstream)
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/)
- [MCP Specification](https://modelcontextprotocol.io/specification)

---

**Template Version**: 1.0
**Last Updated**: 2025-11-25
