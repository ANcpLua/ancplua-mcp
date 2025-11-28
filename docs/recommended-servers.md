# Recommended External MCP Servers

This repo is designed to **cooperate** with high-quality external MCP servers, not replace them.

## .NET Ecosystem

| Server | Purpose | Repo |
|--------|---------|------|
| .NET Types Explorer | Reflect over assemblies, NuGet packages | [V0v1kkk/DotNetMetadataMcpServer](https://github.com/V0v1kkk/DotNetMetadataMcpServer) |
| ContextKeeper | Long-lived workspace memory + Roslyn navigation | [chasecuppdev/contextkeeper-mcp](https://github.com/chasecuppdev/contextkeeper-mcp) |
| Roslyn MCP | Semantic validation, diagnostics | [egorpavlikhin/roslyn-mcp](https://github.com/egorpavlikhin/roslyn-mcp) |
| NuGet Context | NuGet metadata & dependency graphs | [plucked/nuget-context-server](https://github.com/plucked/nuget-context-server) |

## Code Intelligence

| Server | Purpose | Repo |
|--------|---------|------|
| XRAY MCP | AST-based, language-agnostic code intel | [xray-app/xray-mcp](https://github.com/xray-app/xray-mcp) |
| GitHub Semantic Search | RAG over GitHub repos | [edelauna/github-semantic-search-mcp](https://github.com/edelauna/github-semantic-search-mcp) |

## Debugging & Observability

| Server | Purpose | Repo |
|--------|---------|------|
| mcp-debugger | Debug Adapter Protocol bridge | [debugmcpdev/mcp-debugger](https://github.com/debugmcpdev/mcp-debugger) |
| OTEL MCP | Query OpenTelemetry traces/metrics/logs | [shiftyp/otel-mcp-server](https://github.com/shiftyp/otel-mcp-server) |
| Scout Monitoring | APM metrics/logs/errors | [scoutapp/scout-mcp-local](https://github.com/scoutapp/scout-mcp-local) |

## Data & Notebooks

| Server | Purpose | Repo |
|--------|---------|------|
| Jupyter MCP | Python notebooks for analysis | [datalayer/jupyter-mcp-server](https://github.com/datalayer/jupyter-mcp-server) |

## Example Configuration

See [`docs/examples/`](examples/) for MCP client configs that combine these with ancplua-mcp servers.
