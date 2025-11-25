# ADR-003: Debug MCP Tools Architecture

## Architecture Decision Record

### Title
Debug MCP Tools as Shared Library

### Status
Accepted

### Date
2025-11-25

## Context

MCP servers in this repository lack debugging and introspection capabilities. When troubleshooting:
- Configuration issues (environment variables, API keys)
- Transport problems (HTTP headers, authentication)
- Execution flow (tool invocation timing, errors)

Developers must rely on external logging tools or manual inspection. This creates friction during development and makes production debugging difficult.

**Key requirements:**
1. Real-time execution tracing via MCP logging protocol
2. Environment variable inspection (with sensitive value masking)
3. HTTP context inspection for HTTP-based servers
4. Consistent debugging experience across all servers
5. Graceful degradation when features aren't applicable (e.g., HTTP context on stdio transport)

## Decision

We will create a shared `Ancplua.Mcp.DebugTools` library that provides:

1. **Introspection Tools** - Static tools for environment, server info, HTTP context
2. **Logging Extensions** - Extension methods for MCP client logging integration
3. **Annotation Helpers** - Utilities for LLM-targeted debug annotations

Servers opt-in by:
```csharp
builder.Services.AddMcpServer()
    .WithTools<DebugTools>();
```

## Rationale

**Why a shared library (not inline per-server)?**
- **DRY principle**: Single implementation, multiple consumers
- **Follows ServiceDefaults pattern**: Infrastructure code belongs in shared projects
- **Consistent API**: All servers expose identical debug tools
- **Independent versioning**: Debug tools can evolve without touching server code

**Why opt-in (not automatic)?**
- Production servers may want to disable debug tools for security
- Reduces attack surface when not needed
- Explicit registration matches existing tool patterns

## Consequences

### Positive Consequences
- Unified debugging experience across all MCP servers
- Faster troubleshooting of configuration and transport issues
- Follows existing patterns (ServiceDefaults, shared projects)
- Security-conscious design with environment variable masking

### Negative Consequences
- Additional project to maintain
- Servers must explicitly register debug tools
- HTTP-specific tools return degraded responses on stdio transport

### Neutral Consequences
- No runtime overhead when tools aren't invoked
- Requires MCP client support for logging (most clients support this)

## Alternatives Considered

### Alternative 1: Inline Debug Tools Per Server
Add debug tools directly to each server's Tools folder.

**Pros:**
- No new project dependency
- Server-specific customization

**Cons:**
- Code duplication across 4+ servers
- Inconsistent implementations over time
- Violates DRY principle

**Decision:** Rejected - duplication and drift are unacceptable

### Alternative 2: Add to ServiceDefaults
Extend ServiceDefaults to include debug tools.

**Pros:**
- Uses existing shared project
- Automatic inclusion

**Cons:**
- ServiceDefaults is for infrastructure, not MCP tools
- Forces debug tools on all servers (security concern)
- Mixes concerns (OpenTelemetry vs MCP protocol)

**Decision:** Rejected - violates separation of concerns

### Alternative 3: External Debug Server
Create a dedicated MCP server just for debugging.

**Pros:**
- Complete isolation
- Could debug multiple servers

**Cons:**
- Cannot inspect in-process state
- Additional deployment complexity
- Network overhead

**Decision:** Rejected - loses in-process debugging benefit

## Implementation Notes

1. Create `src/Ancplua.Mcp.DebugTools/` project
2. Reference from servers that need debug capability
3. HTTP context tools require `IHttpContextAccessor` registration
4. Environment tools mask common secret patterns (TOKEN, KEY, SECRET, PASSWORD)

## Related Decisions

- [ADR-002] Dual Server Architecture - explains stdio vs HTTP transport
- [spec-005] Debug MCP Tools Specification - detailed tool contracts

## References

- [MCP Logging Protocol](https://modelcontextprotocol.io/specification/server/utilities/logging)
- [.NET MCP SDK Examples](https://github.com/modelcontextprotocol/csharp-sdk)
- Similar pattern in Gemini and Claude debugging tools

---

**Template Version**: 1.0
**Last Updated**: 2025-11-25
