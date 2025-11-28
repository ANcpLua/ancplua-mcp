# ancplua-mcp

C#/.NET MCP servers for real dev workflows. Stdio + HTTP servers built on the official C# MCP SDK, exposing tools for filesystem, git, CI, Roslyn, and multi-AI orchestration.

## Servers

| Server | Transport | Purpose |
|--------|-----------|---------|
| WorkstationServer | stdio | Filesystem, git, CI, Roslyn tools |
| Gateway | HTTP | Remote/multi-tenant hosting |
| AIServicesServer | stdio | Multi-AI orchestration |
| GitHubAppsServer | stdio | GitHub App integrations |
| RoslynMetricsServer | stdio | Code metrics and analysis |

All servers share `Ancplua.Mcp.ServiceDefaults` (OpenTelemetry, health checks, resilience).

## Quick Start

```bash
# Build and test
dotnet build
dotnet test

# Or run the full local CI validation script (mirrors CI)
./tooling/scripts/local-validate.sh

# Run WorkstationServer (stdio)
dotnet run --project src/Servers/Stdio/Workstation/Ancplua.Mcp.Servers.Stdio.Workstation.csproj

# Run HttpServer
dotnet run --project src/Servers/Http/Gateway/Ancplua.Mcp.Servers.Http.Gateway.csproj

# Run AIServicesServer
dotnet run --project src/Servers/Stdio/AIServices/Ancplua.Mcp.Servers.Stdio.AIServices.csproj
```

## Configuration

Example MCP client configs: [`docs/examples/`](docs/examples/)

Machine-readable server inventory: [`docs/servers.json`](docs/servers.json)

Active repository config: [`.mcp.json`](.mcp.json)

## Multi-AI Code Review

Every PR is reviewed by five AI systems:

| Tool | Reviews | Comments | Creates Fix PRs |
|------|---------|----------|-----------------|
| Claude | Yes | Yes | No |
| Jules | Yes | Yes | Yes (needs approval) |
| Copilot | Yes | Yes | No |
| Gemini | Yes | Yes | No |
| CodeRabbit | Yes | Yes | No |

If multiple AIs flag the same issue, it's high confidence. See [`CLAUDE.md`](CLAUDE.md) for details.

## Documentation

| Document | Purpose |
|----------|---------|
| [Architecture](docs/ARCHITECTURE.md) | System design and patterns |
| [Tool Contracts](docs/tool-contracts.md) | MCP tool API reference |
| [Recommended Servers](docs/recommended-servers.md) | External MCP servers to use alongside |
| [Development Guide](docs/development.md) | NuGet workflow, code style |
| [Changelog](CHANGELOG.md) | Version history |
