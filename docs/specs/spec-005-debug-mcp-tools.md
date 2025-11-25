# spec-005: Debug MCP Tools

## Title
Debug MCP Tools Specification

## Status
Accepted

## Context
MCP servers need debugging capabilities for troubleshooting configuration, transport, and execution issues. This specification defines a set of debug tools that provide introspection into server state, environment, and request context.

See [ADR-003](../decisions/adr-003-debug-mcp-tools.md) for architectural rationale.

## Specification

### Overview
The Debug MCP Tools library provides three categories of debugging capability:

1. **Introspection Tools** - Examine environment, server info, HTTP context
2. **Logging Extensions** - Real-time execution tracing via MCP protocol
3. **Annotation Helpers** - LLM-targeted debug information

### Requirements

#### Functional Requirements
- [FR-1] `debug_print_env` - Return environment variables with sensitive value masking
- [FR-2] `debug_get_server_info` - Return server metadata (version, transport, uptime)
- [FR-3] `debug_get_http_headers` - Return HTTP request headers (HTTP transport only)
- [FR-4] `debug_get_user_claims` - Return authentication claims (HTTP transport only)
- [FR-5] HTTP tools gracefully degrade on stdio transport with "No HTTP context available"
- [FR-6] Logging extension enables real-time trace streaming to MCP client

#### Non-Functional Requirements
- [NFR-1] No runtime overhead when tools aren't invoked
- [NFR-2] Sensitive environment variables masked by default
- [NFR-3] JSON output formatted with indentation for readability
- [NFR-4] All tools return structured DTOs, not raw strings

### Design

#### Components

```
Ancplua.Mcp.DebugTools/
├── DebugTools.cs           # Main tool class
├── Models/
│   ├── EnvironmentInfo.cs  # Environment variable DTO
│   ├── ServerInfo.cs       # Server metadata DTO
│   └── HttpContextInfo.cs  # HTTP context DTO
└── Extensions/
    └── McpLoggingExtensions.cs  # ILogger extensions
```

#### Interfaces

##### Tool: debug_print_env

```csharp
[McpServerTool(Name = "debug_print_env")]
[Description("Returns all environment variables with sensitive values masked")]
public EnvironmentInfo PrintEnvironment();
```

**Output DTO:**
```csharp
public record EnvironmentInfo
{
    public Dictionary<string, string> Variables { get; init; }
    public int TotalCount { get; init; }
    public int MaskedCount { get; init; }
}
```

**Masking Rules:**
Variables containing these patterns are masked with `***MASKED***`:
- `TOKEN`, `KEY`, `SECRET`, `PASSWORD`, `CREDENTIAL`, `API_KEY`, `APIKEY`
- `CONNECTION_STRING`, `CONNECTIONSTRING`
- `PRIVATE`, `AUTH`

##### Tool: debug_get_server_info

```csharp
[McpServerTool(Name = "debug_get_server_info")]
[Description("Returns server metadata including version, transport type, and runtime info")]
public ServerInfo GetServerInfo();
```

**Output DTO:**
```csharp
public record ServerInfo
{
    public string ServerName { get; init; }
    public string Version { get; init; }
    public string Transport { get; init; }  // "stdio" | "http" | "sse"
    public string DotNetVersion { get; init; }
    public string OperatingSystem { get; init; }
    public int ProcessorCount { get; init; }
    public string WorkingDirectory { get; init; }
    public TimeSpan Uptime { get; init; }
}
```

##### Tool: debug_get_http_headers

```csharp
[McpServerTool(Name = "debug_get_http_headers")]
[Description("Returns HTTP request headers (HTTP transport only)")]
public HttpContextInfo GetHttpHeaders();
```

**Output DTO:**
```csharp
public record HttpContextInfo
{
    public bool Available { get; init; }
    public string? Message { get; init; }  // Set when !Available
    public Dictionary<string, string>? Headers { get; init; }
    public string? Method { get; init; }
    public string? Path { get; init; }
    public string? QueryString { get; init; }
}
```

##### Tool: debug_get_user_claims

```csharp
[McpServerTool(Name = "debug_get_user_claims")]
[Description("Returns authenticated user claims (HTTP transport only)")]
public UserClaimsInfo GetUserClaims();
```

**Output DTO:**
```csharp
public record UserClaimsInfo
{
    public bool Available { get; init; }
    public bool IsAuthenticated { get; init; }
    public string? AuthenticationType { get; init; }
    public string? Message { get; init; }
    public Dictionary<string, string>? Claims { get; init; }
}
```

#### Logging Extension Usage

Tools can use the MCP client logging to stream debug info:

```csharp
[McpServerTool]
public async Task<string> SomeOperation(
    RequestContext<CallToolRequestParams> context,
    string param)
{
    var logger = context.Server.AsClientLoggerProvider()
        .CreateLogger("SomeOperation");

    logger.LogDebug("Starting operation with param: {Param}", param);
    // ... operation ...
    logger.LogInformation("Operation completed");

    return "result";
}
```

#### Behavior

1. **Environment Masking**: Applied before JSON serialization
2. **HTTP Context**: Obtained via `IHttpContextAccessor` DI
3. **Graceful Degradation**: HTTP tools return `Available = false` on non-HTTP transports
4. **JSON Formatting**: All DTOs serialize with `WriteIndented = true`

### Implementation Considerations

**Project Structure:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" />
    <PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" />
  </ItemGroup>
</Project>
```

**DI Registration in HTTP Servers:**
```csharp
builder.Services.AddHttpContextAccessor();  // Required for HTTP tools
builder.Services.AddMcpServer()
    .WithTools<DebugTools>();
```

**DI Registration in Stdio Servers:**
```csharp
builder.Services.AddMcpServer()
    .WithTools<DebugTools>();  // HTTP tools will gracefully degrade
```

### Testing

1. **Unit Tests**: Test each tool in isolation with mocked IHttpContextAccessor
2. **Integration Tests**: Verify tools work end-to-end with both transports
3. **Masking Tests**: Verify all sensitive patterns are correctly masked
4. **Degradation Tests**: Verify HTTP tools return proper messages on stdio

### Security Considerations

1. **Environment Masking**: Sensitive values never exposed in plaintext
2. **HTTP Headers**: May contain auth tokens - consider additional masking
3. **Production Disable**: Servers can omit `WithTools<DebugTools>()` in production
4. **No Write Operations**: All tools are read-only introspection

### Performance Considerations

1. **Lazy Evaluation**: Environment variables read only when tool invoked
2. **No Background Polling**: Tools are request-response only
3. **Minimal Memory**: DTOs are small, disposable objects
4. **No Caching**: Fresh data on each invocation (correctness over performance)

## Alternatives Considered

See [ADR-003](../decisions/adr-003-debug-mcp-tools.md) for detailed alternatives analysis.

## Dependencies

- `ModelContextProtocol` NuGet package (C# MCP SDK)
- `Microsoft.AspNetCore.Http.Abstractions` for IHttpContextAccessor
- `Ancplua.Mcp.ServiceDefaults` for shared infrastructure patterns

## Timeline
- 2025-11-25 - Draft created
- 2025-11-25 - Accepted
- 2025-11-25 - Implementation started

## References

- [MCP Logging Specification](https://modelcontextprotocol.io/specification/server/utilities/logging)
- [.NET MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [ADR-003](../decisions/adr-003-debug-mcp-tools.md)

## Appendix

### Tool Name Registry

| Tool Name | Server Applicability |
|-----------|---------------------|
| `debug_print_env` | All |
| `debug_get_server_info` | All |
| `debug_get_http_headers` | HTTP (degrades on stdio) |
| `debug_get_user_claims` | HTTP (degrades on stdio) |

### Masked Environment Variable Patterns

```
TOKEN, KEY, SECRET, PASSWORD, CREDENTIAL
API_KEY, APIKEY, PRIVATE, AUTH
CONNECTION_STRING, CONNECTIONSTRING
```
