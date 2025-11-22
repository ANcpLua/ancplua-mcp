# Architecture Documentation

## Overview

The ancplua-mcp project provides Model Context Protocol (MCP) servers that expose development workflow tools to AI assistants and other MCP clients. The architecture is designed around two complementary server implementations sharing common tool implementations.

## Design Principles

1. **Modularity**: Tools are implemented as independent classes that can be reused across server implementations
2. **Simplicity**: Small, focused components that do one thing well
3. **Testability**: All components are designed to be easily testable
4. **Security**: Tools operate with the permissions of the running process; no privilege escalation
5. **Extensibility**: Easy to add new tools or server implementations

## System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    MCP Clients                           │
│  (Claude Desktop, IDEs, Custom Agents)                   │
└─────────────┬───────────────────────┬───────────────────┘
              │                       │
      stdio   │                       │  HTTP/JSON
              │                       │
┌─────────────▼─────────┐   ┌────────▼──────────────────┐
│  WorkstationServer    │   │     HttpServer            │
│  (Console App)        │   │  (ASP.NET Core)           │
└─────────────┬─────────┘   └────────┬──────────────────┘
              │                      │
              └──────────┬───────────┘
                         │
              ┌──────────▼────────────┐
              │      Tool Layer       │
              ├───────────────────────┤
              │  FileSystemTools      │
              │  GitTools             │
              │  CiTools              │
              └───────────────────────┘
                         │
              ┌──────────▼────────────┐
              │   Operating System    │
              │  (Filesystem, Git,    │
              │   Process Execution)  │
              └───────────────────────┘
```

## Components

### WorkstationServer

**Purpose**: Stdio-based MCP server for local development environments.

**Technology**: .NET Console Application

**Communication**: 
- Input: stdin (JSON-RPC over stdio)
- Output: stdout (JSON responses)
- Logging: stderr

**Use Cases**:
- Direct integration with Claude Desktop
- Command-line tool integration
- Local development workflows

**Key Features**:
- Lightweight process
- No network overhead
- Secure by default (only local access)

### HttpServer

**Purpose**: HTTP-based MCP server for web-based integrations.

**Technology**: ASP.NET Core Web API

**Communication**:
- Protocol: HTTP/HTTPS
- Format: JSON
- Endpoints: RESTful API

**Use Cases**:
- Web-based IDE integrations
- Remote development environments
- Microservices architectures

**Key Features**:
- OpenAPI/Swagger documentation
- Health check endpoints
- Scalable and stateless

### Tool Layer

The tool layer contains reusable implementations of development workflow operations:

#### FileSystemTools

Provides filesystem operations:
- Reading and writing files
- Listing directories
- Creating and deleting files/directories
- Checking existence

**Security Considerations**:
- Operates with process permissions
- No automatic path sanitization (by design for flexibility)
- Clients should validate paths

#### GitTools

Provides git repository operations:
- Status and diff
- Log and history
- Branch management
- Staging and committing

**Implementation**:
- Uses `Process` to execute git commands
- Parses git output
- Error handling for git failures

#### CiTools

Provides CI/CD and build operations:
- Building projects (`dotnet build`)
- Running tests (`dotnet test`)
- Restoring dependencies (`dotnet restore`)
- Custom command execution
- System diagnostics

**Security Considerations**:
- Can execute arbitrary commands
- Should be used only in trusted environments
- Command output includes exit codes

## Communication Protocols

### MCP Protocol

Both servers implement the Model Context Protocol (MCP) for tool exposure and invocation.

**Message Format**: JSON-RPC 2.0

**Tool Discovery**: Clients can query available tools and their schemas

**Tool Invocation**: Clients send tool name and parameters; server executes and returns results

### Stdio Protocol (WorkstationServer)

```
Client -> Server: JSON-RPC request on stdin
Server -> Client: JSON-RPC response on stdout
Server -> Logs: Diagnostic messages on stderr
```

### HTTP Protocol (HttpServer)

```
GET /mcp/tools          - List available tools
POST /mcp/execute       - Execute a tool (future)
GET /health             - Health check
GET /openapi            - OpenAPI specification
```

## Data Flow

### Tool Execution Flow

1. Client sends tool invocation request
2. Server validates request and parameters
3. Server invokes appropriate tool method
4. Tool executes operation (filesystem, git, process)
5. Tool returns result or throws exception
6. Server formats response (success or error)
7. Server sends response to client

### Error Handling

Errors are handled at multiple levels:

1. **Tool Level**: Tools throw specific exceptions (FileNotFoundException, InvalidOperationException, etc.)
2. **Server Level**: Servers catch exceptions and format as MCP error responses
3. **Client Level**: Clients receive structured error information

## Security Model

### Threat Model

**Trusted Environment Assumption**: The MCP servers are designed to run in trusted development environments.

**Attack Vectors**:
- Path traversal in filesystem operations
- Command injection in CI tools
- Unauthorized access to git repositories

**Mitigations**:
- No privilege escalation
- Operates with user permissions
- Input validation at tool level (future enhancement)
- No automatic network exposure (HttpServer requires explicit configuration)

### Best Practices

1. Run servers with minimal necessary permissions
2. Use HTTPS for HttpServer in production
3. Implement authentication for HttpServer (future enhancement)
4. Validate all input paths and commands
5. Log all operations for audit trails

## Testing Strategy

### Unit Tests

- Test individual tool methods in isolation
- Mock filesystem and process execution
- Verify error handling

### Integration Tests

- Test server startup and configuration
- Test tool invocation through server APIs
- Test error propagation

### End-to-End Tests

- Test full MCP protocol communication
- Test real filesystem and git operations in isolated environments
- Test with actual MCP clients (Claude Desktop)

## Deployment

### WorkstationServer Deployment

1. Build: `dotnet build WorkstationServer/WorkstationServer.csproj`
2. Configure: Add to MCP client configuration
3. Run: MCP client starts process automatically

### HttpServer Deployment

1. Build: `dotnet build HttpServer/HttpServer.csproj`
2. Configure: Set URLs, ports, HTTPS certificates
3. Run: `dotnet run --project HttpServer/HttpServer.csproj`
4. Deploy: Can be containerized or hosted on any .NET-compatible platform

## Future Enhancements

### Planned Features

1. **Authentication**: OAuth2/JWT for HttpServer
2. **Input Validation**: Comprehensive path and command sanitization
3. **Audit Logging**: Structured logging of all operations
4. **Rate Limiting**: Prevent abuse of HTTP endpoints
5. **Tool Plugins**: Dynamic tool loading
6. **Configuration Management**: External configuration files
7. **Telemetry**: Metrics and monitoring

### Extensibility Points

- Add new tool classes in Tools/ directory
- Implement custom MCP protocol handlers
- Add middleware to HttpServer pipeline
- Create specialized server implementations

## References

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
