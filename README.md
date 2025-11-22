# ancplua-mcp

Family of C#/.NET Model Context Protocol (MCP) servers for dev workflows. Stdio + ASP.NET Core servers expose tools for filesystem, git, CI and diagnostics to Claude, IDEs and other agents. Designed as small, well-tested building blocks for AI-assisted development stacks.

## Overview

This repository provides two MCP server implementations:

1. **WorkstationServer** - A stdio-based MCP server for direct integration with Claude Desktop and other MCP clients
2. **HttpServer** - An ASP.NET Core-based HTTP MCP server for web-based integrations

Both servers expose the same set of tools:
- **FileSystemTools** - Read, write, list, and manage files and directories
- **GitTools** - Git operations including status, log, diff, branch management
- **CiTools** - Build, test, restore, and run commands for CI/CD workflows

## Quick Start

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running WorkstationServer

```bash
dotnet run --project src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj
```

### Running HttpServer

```bash
dotnet run --project src/Ancplua.Mcp.HttpServer/Ancplua.Mcp.HttpServer.csproj
```

The HTTP server will start on https://localhost:5001 (or http://localhost:5000).

## Configuration

See the `docs/examples/` directory for MCP configuration examples:
- `claude-workstation.mcp.json` - Claude Desktop configuration for WorkstationServer
- `claude-http.mcp.json` - Claude Desktop configuration for HttpServer
- `rider-workstation.mcp.json` - JetBrains Rider configuration

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architecture documentation.

## Project Structure

```
ancplua-mcp/
├── src/
│   ├── Ancplua.Mcp.WorkstationServer/  # Stdio MCP server
│   │   ├── Program.cs
│   │   └── Tools/                      # Tool implementations
│   │       ├── FileSystemTools.cs
│   │       ├── GitTools.cs
│   │       └── CiTools.cs
│   └── Ancplua.Mcp.HttpServer/         # HTTP MCP server
│       ├── Program.cs
│       └── Tools/                      # Tool implementations
├── tests/
│   ├── Ancplua.Mcp.WorkstationServer.Tests/
│   └── Ancplua.Mcp.HttpServer.Tests/
├── docs/                               # Documentation
│   ├── ARCHITECTURE.md
│   ├── specs/                          # Specifications
│   ├── decisions/                      # Architecture Decision Records (ADRs)
│   └── examples/                       # Configuration examples
└── tooling/
    └── scripts/                        # Build and validation scripts
```

## Development

### Prerequisites

- .NET 10.0 SDK or later
- Git

### Local Validation

Run the local validation script before committing:

```bash
./tooling/scripts/local-validate.sh
```

## Contributing

Contributions are welcome! Please read the contributing guidelines before submitting PRs.

## License

MIT License - see [LICENSE](LICENSE) file for details.
