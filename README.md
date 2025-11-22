# ancplua-mcp

Family of C#/.NET Model Context Protocol (MCP) servers for dev workflows. Stdio + ASP.NET Core servers expose tools for filesystem, git, CI and diagnostics to Claude, IDEs and other agents. Designed as small, well-tested building blocks for AI-assisted development stacks.

## Overview

This repository provides two MCP server implementations:

1. **Ancplua.Mcp.WorkstationServer** - A stdio-based MCP server for direct integration with Claude Desktop and other MCP clients
2. **Ancplua.Mcp.HttpServer** - An ASP.NET Core-based HTTP MCP server for web-based integrations

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
dotnet run --project src/Ancplua.Mcp.WorkstationServer
```

### Running HttpServer

```bash
dotnet run --project src/Ancplua.Mcp.HttpServer
```

The HTTP server will start on https://localhost:5001 (or http://localhost:5000).

## Configuration

See the `docs/examples/` directory for MCP configuration examples:
- `claude-workstation.mcp.json` - Claude Desktop configuration for WorkstationServer
- `claude-http.mcp.json` - Claude Desktop configuration for HttpServer
- `rider-workstation.mcp.json` - JetBrains Rider configuration

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architecture documentation and [docs/decisions/](docs/decisions/) for Architecture Decision Records (ADRs).

## Project Structure

```
ancplua-mcp/
├── src/
│   ├── Ancplua.Mcp.WorkstationServer/   # Stdio MCP server
│   │   ├── Program.cs
│   │   └── Tools/                       # Tool implementations
│   │       ├── FileSystemTools.cs
│   │       ├── GitTools.cs
│   │       └── CiTools.cs
│   └── Ancplua.Mcp.HttpServer/          # HTTP MCP server
│       ├── Program.cs
│       └── Tools/                       # Tool implementations
├── tests/
│   ├── Ancplua.Mcp.WorkstationServer.Tests/
│   └── Ancplua.Mcp.HttpServer.Tests/
├── docs/                                # Documentation
│   ├── ARCHITECTURE.md
│   ├── specs/                           # Specifications
│   ├── decisions/                       # Architecture Decision Records (ADRs)
│   └── examples/                        # Configuration examples
├── tooling/
│   └── scripts/                         # Build and validation scripts
└── .github/
    └── workflows/                       # CI/CD workflows
```

## Development

### Prerequisites

- .NET 9.0 SDK or later (pinned in `global.json`)
- Git

### Local Validation

Run the local validation script before committing:

```bash
./tooling/scripts/local-validate.sh
```

This script will:
- Restore dependencies
- Build the solution
- Run all tests
- Check for common issues

### Documentation

For any changes that affect external behavior:

1. Update `CHANGELOG.md` under the `[Unreleased]` section
2. Create or update specifications in `docs/specs/`
3. Create or update ADRs in `docs/decisions/` for architectural changes
4. Update this README if project structure or usage changes

See `.github/copilot-instructions.md` for detailed development guidelines and best practices.

## CI/CD

The repository uses GitHub Actions for continuous integration:
- **Build and Test**: Runs on all pushes and pull requests
- **Code Quality**: Runs `dotnet format` checks
- **Security Scan**: Uses Trivy for vulnerability scanning

Dependabot automatically checks for:
- NuGet package updates (weekly)
- GitHub Actions updates (weekly)

## Contributing

Contributions are welcome! Please:
1. Read `.github/copilot-instructions.md` for development guidelines
2. Follow the existing code style and patterns
3. Add tests for new functionality
4. Update documentation as needed
5. Run the local validation script before submitting PRs

## License

MIT License - see [LICENSE](LICENSE) file for details.
