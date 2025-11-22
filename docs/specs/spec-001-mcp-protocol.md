# MCP Protocol Implementation Specification

## Title
Model Context Protocol (MCP) Implementation for ancplua-mcp Servers

## Status
Draft

## Context
The ancplua-mcp servers need to implement the Model Context Protocol to expose development tools to AI assistants like Claude Desktop. Currently, the servers have basic scaffolding but need a complete MCP protocol implementation to be functional.

## Specification

### Overview
This specification defines how the WorkstationServer and HttpServer implement the MCP protocol for tool discovery and invocation.

### Requirements

#### Functional Requirements
- [FR-1] Servers must implement MCP protocol version 1.0 or later
- [FR-2] Servers must expose tool discovery via the `tools/list` RPC method
- [FR-3] Servers must support tool invocation via the `tools/call` RPC method
- [FR-4] Servers must handle JSON-RPC 2.0 format for all messages
- [FR-5] Tool responses must include results or structured error information

#### Non-Functional Requirements
- [NFR-1] Tool invocation must complete within 30 seconds or return a timeout error
- [NFR-2] Servers must log all tool invocations for debugging
- [NFR-3] Error messages must be clear and actionable

### Design

#### Components
- **MCP Protocol Handler**: Parses JSON-RPC messages and dispatches to tool handlers
- **Tool Registry**: Maintains list of available tools and their schemas
- **Tool Executor**: Invokes tool methods and formats responses

#### Interfaces

**Tool Discovery Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list"
}
```

**Tool Discovery Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "filesystem/read",
        "description": "Read contents of a file",
        "inputSchema": {
          "type": "object",
          "properties": {
            "path": { "type": "string" }
          },
          "required": ["path"]
        }
      }
    ]
  }
}
```

**Tool Invocation Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "filesystem/read",
    "arguments": {
      "path": "/path/to/file"
    }
  }
}
```

**Tool Invocation Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": "file contents here"
  }
}
```

#### Data Models
- Tool definitions include name, description, and JSON Schema for parameters
- All responses follow JSON-RPC 2.0 structure
- Errors use standard JSON-RPC error codes

#### Behavior
1. Server starts and initializes tool registry
2. Client connects and sends tool discovery request
3. Server responds with list of available tools
4. Client sends tool invocation request
5. Server validates parameters against schema
6. Server executes tool and captures result
7. Server sends formatted response

### Implementation Considerations
- Use System.Text.Json for JSON serialization
- Implement async/await for all I/O operations
- Use CancellationToken for timeout support
- Consider using Source Generators for JSON serialization performance

### Testing
- Unit tests for JSON-RPC message parsing
- Integration tests for tool discovery
- End-to-end tests with actual MCP clients
- Error handling tests for invalid requests

### Security Considerations
- Validate all input parameters before execution
- Sanitize file paths to prevent directory traversal
- Implement rate limiting for tool invocations
- Log all tool calls for audit trail

### Performance Considerations
- Response time target: < 100ms for tool discovery
- Response time target: < 5s for typical tool invocations
- Support concurrent tool invocations

## Alternatives Considered

### Alternative 1: Custom Protocol
Instead of MCP, implement a custom protocol specific to ancplua.

**Pros:**
- Full control over features
- Can optimize for specific use cases

**Cons:**
- Requires custom client implementations
- No ecosystem compatibility
- More maintenance burden

**Decision:** Not chosen due to lack of ecosystem compatibility

### Alternative 2: REST API Only
Implement only REST endpoints without JSON-RPC.

**Pros:**
- Simpler implementation
- More familiar to many developers

**Cons:**
- Not compatible with MCP clients
- Doesn't support stdio transport
- Missing standard tool discovery

**Decision:** Not chosen as it doesn't meet MCP requirements

## Dependencies
- System.Text.Json for JSON serialization
- MCP specification version 1.0+
- Tool implementations (FileSystemTools, GitTools, CiTools)

## Timeline
- 2025-11-22 - Draft created
- TBD - Implementation started
- TBD - Implemented and tested

## References
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [JSON Schema](https://json-schema.org/)

## Appendix

### Example Tool Definitions

```csharp
public class ToolDefinition
{
    public string Name { get; set; }
    public string Description { get; set; }
    public JsonSchema InputSchema { get; set; }
}
```

### Error Code Mapping

| Error Condition | JSON-RPC Code | Message |
|----------------|---------------|---------|
| Tool not found | -32601 | Method not found |
| Invalid parameters | -32602 | Invalid params |
| Tool execution error | -32603 | Internal error |
| Timeout | -32000 | Tool execution timeout |
