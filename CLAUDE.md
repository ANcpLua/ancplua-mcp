# Claude Integration Guide

This guide explains how to integrate ancplua-mcp servers with Claude Desktop and other Claude-based tools.

## Overview

The ancplua-mcp servers implement the Model Context Protocol (MCP), allowing Claude to access filesystem, git, and CI/CD tools directly from your development environment.

## WorkstationServer Integration

The WorkstationServer uses stdio for communication, making it ideal for local Claude Desktop integration.

### Configuration for Claude Desktop

1. Locate your Claude Desktop configuration file:
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
   - Linux: `~/.config/Claude/claude_desktop_config.json`

2. Add the WorkstationServer configuration:

```json
{
  "mcpServers": {
    "ancplua-workstation": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/ancplua-mcp/WorkstationServer/WorkstationServer.csproj"
      ]
    }
  }
}
```

3. Restart Claude Desktop

### Available Tools

Once configured, Claude will have access to:

- **File Operations**: Read, write, list, and manage files
- **Git Operations**: Check status, view logs, diff, manage branches
- **CI Operations**: Build projects, run tests, restore dependencies

## HttpServer Integration

The HttpServer provides an HTTP API for MCP operations, suitable for web-based integrations.

### Starting the Server

```bash
dotnet run --project HttpServer/HttpServer.csproj
```

The server will start on:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

### API Endpoints

- `GET /mcp/tools` - List available tools
- `GET /health` - Health check endpoint

## Security Considerations

⚠️ **Important Security Notes**:

1. The MCP servers provide direct access to your filesystem and can execute commands
2. Only use these servers in trusted development environments
3. Never expose the HttpServer to the public internet without proper authentication
4. Review the tool permissions and capabilities before enabling them

## Usage Examples

### Example 1: Reading a File

Ask Claude:
> "Can you read the contents of the README.md file?"

### Example 2: Git Status

Ask Claude:
> "What's the current git status of this repository?"

### Example 3: Running Tests

Ask Claude:
> "Can you run the tests for this project?"

## Troubleshooting

### WorkstationServer Not Connecting

1. Check that the path in the configuration is correct and absolute
2. Verify that .NET is installed and in your PATH
3. Check Claude Desktop logs for error messages

### HttpServer Connection Issues

1. Verify the server is running: `curl http://localhost:5000/health`
2. Check for port conflicts
3. Review server logs for errors

## Advanced Configuration

### Custom Port for HttpServer

Set the port via environment variable or command line:

```bash
dotnet run --project HttpServer/HttpServer.csproj --urls "http://localhost:8080"
```

### Development Mode

For development with hot reload:

```bash
dotnet watch run --project WorkstationServer/WorkstationServer.csproj
```

## Further Reading

- [MCP Specification](https://modelcontextprotocol.io/)
- [Architecture Documentation](docs/ARCHITECTURE.md)
- [Configuration Examples](docs/examples/)
