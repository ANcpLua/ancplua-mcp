# ADR-002: Dual Server Architecture (Stdio and HTTP)

## Architecture Decision Record

### Title
Implement Both Stdio-based and HTTP-based MCP Servers

### Status
Accepted

### Date
2025-11-22

## Context

The ancplua-mcp project needs to expose development tools via the Model Context Protocol (MCP). MCP clients can communicate through different transports:

1. **Stdio (Standard Input/Output)**: Used by Claude Desktop and similar desktop applications
2. **HTTP**: Used by web-based tools, remote systems, and services

We need to decide whether to:
- Build only a stdio server
- Build only an HTTP server
- Build both servers

Key considerations:
- Different MCP clients prefer different transports
- Stdio is secure by default (local only)
- HTTP allows remote access and web integrations
- Development and maintenance cost of multiple servers
- Code sharing opportunities between implementations

## Decision

We will implement **both a stdio-based server (WorkstationServer) and an HTTP-based server (HttpServer)** in the same repository, sharing common tool implementations.

**Structure:**
```
ancplua-mcp/
├── WorkstationServer/  # Console app, stdio transport
├── HttpServer/         # ASP.NET Core, HTTP transport
└── Common Tools/       # Shared implementations
    ├── FileSystemTools.cs
    ├── GitTools.cs
    └── CiTools.cs
```

## Rationale

**Why Both Servers:**

1. **Different Use Cases**:
   - WorkstationServer: Local development with Claude Desktop
   - HttpServer: Web integrations, remote development, IDE plugins

2. **Security Models**:
   - Stdio server is inherently secure (local process only)
   - HTTP server can be secured with authentication when needed

3. **Code Reuse**:
   - Tool implementations are identical
   - Both servers share the same underlying logic
   - Minimal duplication, maximum flexibility

4. **Ecosystem Coverage**:
   - Covers both major MCP transport types
   - Makes ancplua-mcp useful in more scenarios

5. **Maintenance Benefits**:
   - Testing tool logic once benefits both servers
   - Bug fixes apply to both implementations
   - Clear separation of concerns

**Design Principles Applied:**

- **Single Responsibility**: Each server handles one transport
- **DRY (Don't Repeat Yourself)**: Tools are implemented once
- **Flexibility**: Users choose the right server for their needs
- **Simplicity**: Each server is small and focused

## Consequences

### Positive Consequences

- **Wider Adoption**: Support for both major MCP transports
- **Flexibility**: Users pick the server that fits their workflow
- **Code Quality**: Shared tools means consolidated testing
- **Security Options**: Can use stdio for security-sensitive operations
- **Remote Capability**: HTTP server enables remote development scenarios
- **Innovation**: Can experiment with different transport features

### Negative Consequences

- **Increased Complexity**: Two servers to maintain and document
- **Testing Overhead**: Must test both server implementations
- **Documentation**: Need separate setup guides for each server
- **Build Time**: Slightly longer to build both projects
- **Release Coordination**: Both servers should stay in sync

### Neutral Consequences

- **Project Size**: More files and directories
- **Learning Curve**: Users need to choose which server to use
- **CI/CD**: GitHub Actions must build and test both

## Alternatives Considered

### Alternative 1: Stdio Server Only

**Pros:**
- Simpler project structure
- Only one server to maintain
- Secure by default
- Perfect for Claude Desktop integration

**Cons:**
- No remote access capability
- Can't integrate with web-based tools
- Limited to local development
- Misses HTTP MCP client use cases

**Decision:** Not chosen because it limits use cases and HTTP support is valuable

### Alternative 2: HTTP Server Only

**Pros:**
- Single server to maintain
- Works for both local and remote
- REST APIs are widely understood
- Can be containerized easily

**Cons:**
- Overhead for local-only use cases
- Security concerns for local development
- Not ideal for Claude Desktop integration
- Requires network configuration

**Decision:** Not chosen because stdio is the preferred transport for desktop MCP clients like Claude

### Alternative 3: Single Server with Both Transports

Create one application that supports both stdio and HTTP simultaneously

**Pros:**
- Single codebase
- One server process
- Unified configuration

**Cons:**
- Complex startup logic
- Mixing concerns (console and web server)
- Harder to test
- Configuration complexity
- Not aligned with .NET project templates

**Decision:** Not chosen due to complexity and mixing of concerns

### Alternative 4: Separate Repositories

Put WorkstationServer and HttpServer in separate repositories

**Pros:**
- Independent versioning
- Separate issue tracking
- Focused documentation

**Cons:**
- Code duplication (tools)
- Separate testing infrastructure
- Harder to keep in sync
- More overhead for contributors

**Decision:** Not chosen because code sharing is valuable

## Implementation Notes

### Code Sharing Strategy

Tools are implemented in each server's Tools/ directory:
- WorkstationServer/Tools/FileSystemTools.cs
- HttpServer/Tools/FileSystemTools.cs

Initially, files are identical copies. Future enhancement could extract to a shared class library if the duplication becomes problematic.

**Current Approach (Accepted):**
```
WorkstationServer/Tools/*.cs  ← Tool implementations
HttpServer/Tools/*.cs         ← Same implementations
```

**Future Approach (If Needed):**
```
AncpluaMcp.Tools/*.cs         ← Shared class library
WorkstationServer/            ← References shared library
HttpServer/                   ← References shared library
```

### Testing Strategy

- **Unit Tests**: Test tools in isolation (once per server)
- **Integration Tests**: Test server-specific behavior
- **E2E Tests**: Test with actual MCP clients for each transport

### Documentation Strategy

- Main README: Explains both servers
- CLAUDE.md: Setup for both server types
- ARCHITECTURE.md: Describes dual-server pattern
- Individual examples: One config per server type

### Deployment Options

**WorkstationServer:**
- Direct execution: `dotnet run`
- Published executable
- Configured in MCP client config

**HttpServer:**
- Direct execution: `dotnet run`
- Docker container
- Hosted on IIS/Kestrel
- Cloud deployment (Azure, AWS, etc.)

## Related Decisions

- ADR-001: Use .NET 9 (both servers benefit from modern features)
- Spec-001: MCP Protocol (both servers implement same protocol)

## References

- [Model Context Protocol Transports](https://modelcontextprotocol.io/specification/transports)
- [ASP.NET Core Web APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [.NET Console Applications](https://learn.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio)

## Notes

**For Users:**
- Use WorkstationServer for Claude Desktop and local tools
- Use HttpServer for web integrations and remote scenarios
- Both expose identical tools with same capabilities

**For Contributors:**
- Keep tool implementations synchronized
- Test changes in both servers
- Update documentation for both

**Future Considerations:**
- If tool duplication becomes problematic, extract to shared library
- Consider WebSocket transport in future
- Evaluate gRPC transport for high-performance scenarios

---

**Template Version**: 1.0  
**Last Updated**: 2025-11-22
