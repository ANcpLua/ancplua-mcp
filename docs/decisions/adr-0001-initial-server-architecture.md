# ADR-0001: Initial Server Architecture

## Architecture Decision Record

### Title
Dual-Server Architecture for MCP Protocol Support

### Status
Accepted

### Date
2025-11-22

## Context

The ancplua-mcp repository needs to provide Model Context Protocol (MCP) server implementations that support different integration scenarios:

1. **Local development workflows**: Direct integration with Claude Desktop and other MCP clients that communicate via stdio
2. **Remote/web-based scenarios**: Integration with web applications, remote clients, and services that require HTTP-based communication

Key requirements:
- Support both stdio and HTTP transport mechanisms
- Share common tool implementations across both servers
- Maintain clear separation of concerns
- Enable independent deployment and scaling
- Follow .NET best practices and conventions

Key constraints:
- Must use the official C# MCP SDK
- Must support .NET 8.0 or later
- Tools should be testable independently of transport mechanism
- Architecture should be simple enough for easy maintenance

## Decision

We will implement a dual-server architecture with two separate MCP server projects:

1. **Ancplua.Mcp.WorkstationServer**: A console application using stdio transport
   - Uses `ModelContextProtocol` package
   - Implements `AddMcpServer()` with `WithStdioServerTransport()`
   - Suitable for local development tools and IDE integration

2. **Ancplua.Mcp.HttpServer**: An ASP.NET Core web application using HTTP transport
   - Uses `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` packages
   - Implements `AddMcpServer()` with `WithHttpTransport()` and `MapMcp()`
   - Suitable for web-based integrations and remote access

Both servers will:
- Share the same tool implementations (FileSystemTools, GitTools, CiTools)
- Use dependency injection for extensibility
- Follow the C# MCP SDK patterns with `[McpServerToolType]` and `[McpServerTool]` attributes
- Target .NET 9.0 (with backward compatibility for .NET 8.0)

## Rationale

### Why Two Separate Servers?

1. **Different deployment models**: Stdio servers run as child processes, while HTTP servers run as web applications. Combining them would complicate deployment and configuration.

2. **Clear separation of concerns**: Each server has a single responsibility and transport mechanism. This makes the codebase easier to understand and maintain.

3. **Independent scaling**: HTTP servers can be scaled horizontally in cloud environments, while stdio servers are inherently single-user. Separate projects allow different scaling strategies.

4. **Simpler dependencies**: WorkstationServer doesn't need ASP.NET Core dependencies, keeping it lightweight for local use.

### Why Share Tool Implementations?

1. **Consistency**: Users get the same tool behavior regardless of which server they use.

2. **Maintainability**: Bug fixes and improvements only need to be made once.

3. **Testability**: Tools can be tested independently of transport mechanisms.

### Why Use the Official C# MCP SDK?

1. **Standards compliance**: Ensures compatibility with all MCP clients.

2. **Reduced maintenance**: The SDK handles protocol details, versioning, and edge cases.

3. **Community support**: Benefit from SDK improvements and bug fixes.

4. **Best practices**: SDK provides conventions and patterns for tool implementation.

## Consequences

### Positive Consequences

- **Flexibility**: Supports both local and remote integration scenarios
- **Simplicity**: Each server has a focused purpose and minimal dependencies
- **Maintainability**: Shared tool implementations reduce duplication
- **Testability**: Tools and servers can be tested independently
- **Standards compliance**: Official SDK ensures compatibility
- **Performance**: Stdio server is lightweight for local use; HTTP server can be scaled

### Negative Consequences

- **Duplication of server setup code**: Some boilerplate is duplicated between servers (acceptable trade-off)
- **Multiple projects to maintain**: Increases repository complexity slightly
- **Deployment considerations**: Users need to understand which server to use for their scenario

### Neutral Consequences

- **Testing complexity**: Need separate test projects for each server
- **Documentation requirement**: Must document when to use each server
- **CI pipeline**: Need to build and test both servers

## Alternatives Considered

### Alternative 1: Single Server with Configurable Transport

**Description**: Create one MCP server project that can be configured at runtime to use either stdio or HTTP transport.

**Pros:**
- Single codebase to maintain
- Single deployment artifact
- No code duplication

**Cons:**
- More complex configuration
- Heavier dependencies (all servers need ASP.NET Core)
- Harder to understand which transport is active
- Complicates deployment (more configuration options)
- Less obvious which deployment model to use

**Decision:** Rejected. The added complexity and coupling outweigh the benefits of a single codebase.

### Alternative 2: Shared Library with Thin Server Wrappers

**Description**: Create a shared library with all tool implementations, then create minimal server projects that just wire up the transport.

**Pros:**
- Maximum code reuse
- Clear separation between tools and transport
- Could support additional transports easily

**Cons:**
- Three projects instead of two (adds complexity)
- Adds indirection that may not be necessary
- Complicates project references and testing
- Over-engineering for current needs

**Decision:** Rejected. The current dual-server approach with tools embedded in each server is simpler and sufficient for our needs. We can refactor to a shared library if we add more servers in the future.

### Alternative 3: Stdio-Only Server

**Description**: Only implement the stdio-based WorkstationServer and defer HTTP support.

**Pros:**
- Simplest possible implementation
- Fastest to deliver
- Minimal maintenance

**Cons:**
- Doesn't support remote/web-based integration scenarios
- Would require significant work to add HTTP support later
- Limits use cases and adoption

**Decision:** Rejected. HTTP support is a core requirement for web-based integrations.

## Implementation Notes

### Project Structure
- Both servers live under `src/` directory
- Follow .NET naming convention: `Ancplua.Mcp.<ServerType>`
- Each server has corresponding test project under `tests/`

### Tool Organization
- Tools are organized by category in `Tools/` subdirectory
- Use static classes with `[McpServerToolType]` attribute
- Keep tool implementations simple and focused

### Migration Path
- No migration needed (this is the initial architecture)
- If shared library becomes necessary, tools can be extracted without breaking client code

### Timeline
- Initial implementation: November 2025
- Both servers available from v0.1.0 release

### Breaking Changes
None (initial implementation)

## Related Decisions

- [ADR-0002] - Dual Server Architecture (if exists, else N/A)
- [Spec-001] - MCP Protocol Implementation

## References

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [.NET Console Apps](https://docs.microsoft.com/dotnet/core/tutorials/cli-create-console-app)

## Notes

This ADR documents the foundational architectural decision for the ancplua-mcp repository. The dual-server approach provides flexibility while keeping each server simple and focused. Future ADRs should reference this decision when proposing changes to server architecture.

---

**Template Version**: 1.0  
**Last Updated**: 2025-11-22
