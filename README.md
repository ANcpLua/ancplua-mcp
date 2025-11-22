# ancplua-mcp

Family of C#/.NET Model Context Protocol (MCP) servers for dev workflows. Stdio + ASP.NET Core servers expose tools for filesystem, git, CI and diagnostics to Claude, IDEs and other agents. Designed as small, well-tested building blocks for AI-assisted development stacks.

## Overview

This repository provides four MCP server implementations:

1. **WorkstationServer** - Stdio-based MCP server for local dev tools (filesystem, git, CI)
2. **HttpServer** - ASP.NET Core HTTP MCP server for web-based integrations
3. **AIServicesServer** ⭐ NEW - Orchestrates multiple AI services (Claude, Jules, Gemini, ChatGPT, Copilot, CodeRabbit, Codecov)
4. **GitHubAppsServer** - Direct integration with GitHub Apps and AI code review services

### Core Servers (WorkstationServer & HttpServer)
- **FileSystemTools** - Read, write, list, and manage files and directories
- **GitTools** - Git operations including status, log, diff, branch management
- **CiTools** - Build, test, restore, and run commands for CI/CD workflows

### AI Services Server ⭐ NEW
- **ServiceDiscoveryTools** - List and query available AI services
- **InterServiceTools** - Send requests between AI services
- **WorkflowTools** - Create and execute multi-service AI workflows
- **ContextTools** - Shared context management across services
- **AggregationTools** - Combine results from multiple AI services

Enables Claude to talk to Jules, Gemini to talk to Copilot, and all AI services to collaborate!

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

## GitHub Actions & Code Review

This repository includes automated PR reviews powered by **Jules AI**:

- **Automatic Review**: AI reviews are automatically triggered when PRs are opened, updated, or reopened
- **On-Demand Review**: Comment `/jules-review` on any PR to request an immediate review
- **Customizable Prompts**: Configure review focus (code quality, security, performance, etc.)
- **Code Generation**: Jules can suggest fixes and improvements directly in PRs

### Setup

1. Get a Jules API key from [jules.google.com](https://jules.google.com)
2. Add it as a repository secret named `JULES_API_KEY`
3. Open a PR and watch Jules provide automated feedback

For detailed setup instructions, see [docs/jules-pr-review-setup.md](docs/jules-pr-review-setup.md).

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

## Versioning & Stability

### Version Strategy

This repository follows semantic versioning principles:

* **Major versions** (1.0, 2.0): Breaking changes to tool signatures, removal of tools, or incompatible MCP protocol changes
* **Minor versions** (1.1, 1.2): New tools, new optional parameters, or backward-compatible enhancements
* **Patch versions** (1.0.1, 1.0.2): Bug fixes, performance improvements, documentation updates

### Tool Stability

**Stable Tools**: Tools marked in specs as "Implemented" are considered stable. Changes to these tools follow semantic versioning.

**Experimental Tools**: Tools marked "Experimental" in specs may change without major version bumps. Use with caution in production.

### MCP SDK Preview Notice

⚠️ **Important**: The Model Context Protocol C# SDK is currently in preview (0.4.0-preview.3). Breaking changes may occur in the SDK itself:

* We will update to new MCP SDK versions as they release
* Breaking SDK changes will be documented in `CHANGELOG.md`
* Where possible, we maintain backward compatibility at the tool level even when the SDK changes

### Deprecation Policy

When tools need to be removed or changed incompatibly:

1. Tool is marked **deprecated** in specs and `CHANGELOG.md`
2. Deprecation notice remains for at least one minor version
3. Tool is removed or changed in the next major version

Deprecated tools will log warnings when called but continue to function until removal.

## License

MIT License - see [LICENSE](LICENSE) file for details.
