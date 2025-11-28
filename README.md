# ancplua-mcp

Family of C#/.NET Model Context Protocol (MCP) servers for **real dev workflows**.

- Stdio + ASP.NET Core servers built on the **official C# MCP SDK**.
- Expose tools for filesystem, git, CI, diagnostics, architecture, Roslyn metrics, NuGet, and more.
- Designed as **small, well-tested building blocks** inside a larger AI-assisted .NET stack.   

This repo is the **“.NET spine”**: your own servers live here, while “god-tier” external MCP servers plug in next to them via config.

---

## 1. Servers in this repository

All servers share a common infrastructure project: **`Ancplua.Mcp.ServiceDefaults`** (OpenTelemetry, health checks, resilience, stdio logging discipline).

### 1.1 Core servers

1. **WorkstationServer** (`Ancplua.Mcp.WorkstationServer`)
   - Transport: **stdio**
   - Use: Claude Desktop / Claude Code / IDEs on the **same machine**
   - Tools:
     - `FileSystemTools` – list/read/write files, glob queries
     - `GitTools` – status, branches, short diffs
     - `CiTools` – `dotnet restore/build/test`, local CI scripts
     - `NuGetTools` – inspect packages, feeds, versions (custom)
     - `RoslynTools` – semantic code analysis, diagnostics (custom, read-only)
     - `RoslynMetricsTools` – metrics, code-health signals (custom)
     - `ArchitectureTools` – architecture/ADR/spec helpers (custom)

2. **HttpServer** (`Ancplua.Mcp.HttpServer`)
   - Transport: **HTTP** with `ModelContextProtocol.AspNetCore`
   - Use: remote / shared / multi-tenant hosting
   - Tools: can mirror WorkstationServer tools or expose a subset

3. **AIServicesServer** (`Ancplua.Mcp.AIServicesServer`)
   - Transport: **stdio**
   - Purpose: Orchestrate multiple AI services (Claude, Gemini, ChatGPT, Copilot, CodeRabbit, Codecov, etc.) for PR review & workflows   
   - Tools:
     - `ServiceDiscoveryTools` – list/query AI services
     - Future: Inter-service routing, workflows, context sharing, aggregation

4. **GitHubAppsServer** (`Ancplua.Mcp.GitHubAppsServer`)
   - Transport: **stdio**
   - Purpose: Direct GitHub App integration (AI reviewers, coverage, bots)
   - Tools: GitHub App workflows (planned spec-driven tools)

---

## 2. External “god-tier” MCP servers

This repo is designed to **cooperate** with a curated set of high-quality MCP servers, not to replace them.

Examples (not bundled, but strongly recommended):

- **.NET Types Explorer MCP Server** – Reflect over compiled assemblies, NuGet packages.  
- **ContextKeeper MCP** – Long-lived workspace memory + Roslyn navigation.  
- **Roslyn MCP Server(s)** – Semantic validation, diagnostics, “find usages”.  
- **NuGet Context MCP Server** – NuGet metadata & dependency graphs.  
- **Jupyter MCP Server** – Python notebooks for embeddings, analysis, data.  
- **GitHub Semantic Search MCP** – RAG over GitHub repos.  
- **XRAY MCP** – AST-based, language-agnostic code intelligence.  
- **mcp-debugger** – Debug Adapter Protocol bridge (Python, Node, Rust, etc.).  
- **OTEL MCP Server** – Query OpenTelemetry traces/metrics/logs.  
- **Scout Monitoring MCP** – APM metrics/logs/errors surfaced via MCP.   

Integration is done at the **client config level**, not inside this repo (see section 6).

---

## 3. Architecture snapshot

- All servers share:
  - Official C# MCP SDK (`ModelContextProtocol`, `ModelContextProtocol.AspNetCore`).
  - `Ancplua.Mcp.ServiceDefaults` for observability + resilience.
- Core pattern: **Cohesive tools grouped by concern**, each tool is:
  - Single responsibility
  - JSON-shaped input/output
  - Stable name and signature
- No server calls another server directly; the **MCP client** composes them.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for details.

---

## 4. Quick start

### 4.1 Build and test

```bash
dotnet build
dotnet test
```

Or use the local validation script (mirrors CI):

```bash
./tooling/scripts/local-validate.sh
```

### 4.2 Run WorkstationServer (stdio)

```bash
dotnet run --project src/Servers/Stdio/Workstation/Ancplua.Mcp.Servers.Stdio.Workstation.csproj
```

Configure Claude Desktop / Claude Code or other clients to use this as an MCP server.

### 4.3 Run HttpServer (HTTP MCP)

```bash
dotnet run --project src/Servers/Http/Gateway/Ancplua.Mcp.Servers.Http.Gateway.csproj
```

Defaults to `https://localhost:5001` / `http://localhost:5000` unless overridden. 

### 4.4 Run AIServicesServer

```bash
dotnet run --project src/Servers/Stdio/AIServices/Ancplua.Mcp.Servers.Stdio.AIServices.csproj
```

Or via Docker (if Dockerfiles are present):

```bash
docker build -f Dockerfile.aiservices -t ancplua-ai-services .
docker run -i --rm ancplua-ai-services
```

---

## 5. NuGet dependency management

This repository uses **Central Package Management (CPM)** with **per-project lock files** for deterministic builds.

### Key files

| File | Purpose |
|------|---------|
| `Directory.Packages.props` | Centralized package version definitions |
| `Directory.Build.props` | Enables lock files and locked restore in CI |
| `**/packages.lock.json` | Per-project lock files (one per `.csproj`) |

### How it works

- **All package versions** are defined centrally in `Directory.Packages.props`
- **Individual `.csproj` files** use `<PackageReference Include="..." />` without `Version` attributes
- **Lock files** are generated per project and committed to source control
- **CI enforces locked restore**: if lock files would change, CI fails

### Developer workflow

```bash
# Normal build (lock files respected)
dotnet restore
dotnet build

# When adding/updating packages, lock files regenerate automatically
# Commit the updated packages.lock.json files with your changes
```

### Updating dependencies

1. Update version in `Directory.Packages.props`
2. Run `dotnet restore` — lock files regenerate
3. Run `dotnet build && dotnet test` — verify changes
4. Commit both the `.props` changes and updated `packages.lock.json` files

### CI behavior

CI runs with `RestoreLockedMode=true`. If your PR changes dependencies without updating lock files, CI will fail with a restore error. Always commit lock file changes alongside dependency updates.

---

## 6. Example MCP client configuration

For Claude Desktop / Claude Code (`claude.mcp` or `.mcp.json`):

```jsonc
{
  "mcpServers": {
    "ancplua-workstation": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Servers/Stdio/Workstation/Ancplua.Mcp.Servers.Stdio.Workstation.csproj"
      ]
    },
    "ancplua-http": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Servers/Http/Gateway/Ancplua.Mcp.Servers.Http.Gateway.csproj"
      ]
    },
    "ancplua-ai-services": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Servers/Stdio/AIServices/Ancplua.Mcp.Servers.Stdio.AIServices.csproj"
      ]
    },

    // External “god-tier” servers (examples)
    "dotnet-build-insights": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "C:/dev/dotnet-build-insights/src/DotNet.BuildInsights.McpServer/DotNet.BuildInsights.McpServer.csproj"
      ]
    },
    "dotnet-code-insights": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "C:/dev/dotnet-code-insights/src/DotNet.CodeInsights.McpServer/DotNet.CodeInsights.McpServer.csproj"
      ]
    },
    "dotnet-types-explorer": {
      "type": "stdio",
      "command": "/path/to/DotNetMetadataMcpServer",
      "args": []
    },
    "contextkeeper": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/contextkeeper-mcp/src/ContextKeeper"
      ]
    },
    "jupyter": {
      "type": "stdio",
      "command": "uvx",
      "args": ["jupyter-mcp-server@latest"]
    },
    "xray-mcp": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "xray-mcp"]
    },
    "mcp-debugger": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@debugmcp/mcp-debugger"]
    }
  }
}
```

### 5.1 GitHub Copilot Integration

To use these servers with **GitHub Copilot Coding Agent** (in repository settings):

```json
{
  "mcpServers": {
    "ancplua-workstation": {
      "type": "local",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/Servers/Stdio/Workstation/Ancplua.Mcp.Servers.Stdio.Workstation.csproj"
      ],
      "tools": ["*"]
    },
    "ancplua-ai-services": {
      "type": "local",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/Servers/Stdio/AIServices/Ancplua.Mcp.Servers.Stdio.AIServices.csproj"
      ],
      "tools": ["*"],
      "env": {
        "GITHUB_TOKEN": "COPILOT_MCP_GITHUB_TOKEN",
        "GEMINI_API_KEY": "COPILOT_MCP_GEMINI_API_KEY",
        "JULES_API_KEY": "COPILOT_MCP_JULES_API_KEY"
      }
    }
  }
}
```

*Note: You must create a `copilot` environment in your repo settings and add the secrets (prefixed with `COPILOT_MCP_`).*

---

## 7. Quad-AI Code Review

Every PR in this repository is automatically reviewed by **five AI systems**:

### AI Tool Capabilities

| Tool | Reviews | Comments | Creates Fix PRs | Auto-Fix |
|------|---------|----------|-----------------|----------|
| **Claude** | ✅ | ✅ | ❌ | ❌ |
| **Jules** | ✅ | ✅ | ✅ (needs approval) | ❌ |
| **Copilot** | ✅ | ✅ | ❌ | ❌ |
| **Gemini** | ✅ | ✅ | ❌ | ❌ |
| **CodeRabbit** | ✅ | ✅ | ❌ | ❌ |

### How it works

1. Open a PR → All five AIs review automatically
2. Reviews appear in the GitHub PR sidebar
3. If multiple AIs flag the same issue → high confidence
4. Jules can create fix PRs (requires human approval of its plan)

**The gap:** No AI currently does `detect failure → understand fix → push fix → re-run CI` autonomously.

See `CLAUDE.md` Section 10 for full configuration details.

---

## 8. Machine-friendly MCP server inventory

You can drop this JSON into `docs/servers.json` or similar for agents to parse:

```jsonc
[
  {
    "name": "Ancplua Workstation MCP Server",
    "id": "ancplua-workstation",
    "primaryLanguage": "C#",
    "repo": "https://github.com/ANcpLua/ancplua-mcp",
    "category": ["workstation", "filesystem", "git", "ci"],
    "mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Servers/Stdio/Workstation/Ancplua.Mcp.Servers.Stdio.Workstation.csproj"
      ]
    }
  },
  {
    "name": "Ancplua HTTP MCP Server",
    "id": "ancplua-http",
    "primaryLanguage": "C#",
    "repo": "https://github.com/ANcpLua/ancplua-mcp",
    "category": ["http", "shared", "remote"],
    "mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Servers/Http/Gateway/Ancplua.Mcp.Servers.Http.Gateway.csproj"
      ]
    }
  },
  {
    "name": "Ancplua AI Services MCP Server",
    "id": "ancplua-ai-services",
    "primaryLanguage": "C#",
    "repo": "https://github.com/ANcpLua/ancplua-mcp",
    "category": ["ai-orchestration", "github-apps"],
    "mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Servers/Stdio/AIServices/Ancplua.Mcp.Servers.Stdio.AIServices.csproj"
      ]
    }
  },

  // External “god-tier” servers (unchanged from your list)
  {
    "name": ".NET Types Explorer MCP Server",
    "id": "dotnet-types-explorer",
    "primaryLanguage": "C#",
    "repo": "https://github.com/V0v1kkk/DotNetMetadataMcpServer",
    "category": ["code-intel", "dotnet", "nuget"],
    "mcp": {
      "type": "stdio",
      "command": "/path/to/DotNetMetadataMcpServer",
      "args": []
    }
  },
  {
    "name": "ContextKeeper MCP",
    "id": "contextkeeper",
    "primaryLanguage": "C#",
    "repo": "https://github.com/chasecuppdev/contextkeeper-mcp",
    "category": ["context", "history", "roslyn", "dotnet"],
    "mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/contextkeeper-mcp/src/ContextKeeper"
      ]
    }
  },
  {
    "name": "Roslyn MCP Server",
    "id": "roslyn-mcp",
    "primaryLanguage": "C#",
    "repo": "https://github.com/egorpavlikhin/roslyn-mcp",
    "category": ["code-intel", "analysis", "dotnet"],
    "mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "/path/to/roslyn-mcp/RoslynMCP/RoslynMCP.csproj"
      ]
    }
  },
  {
    "name": "NuGet Context MCP Server",
    "id": "nuget-context",
    "primaryLanguage": "C#",
    "repo": "https://github.com/plucked/nuget-context-server",
    "category": ["nuget", "dependencies"],
    "mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/nuget-context-server/src/NuGetContextServer"
      ]
    }
  },
  {
    "name": "Jupyter MCP Server",
    "id": "jupyter",
    "primaryLanguage": "Python",
    "repo": "https://github.com/datalayer/jupyter-mcp-server",
    "category": ["python", "notebooks", "data"],
    "mcp": {
      "type": "stdio",
      "command": "uvx",
      "args": ["jupyter-mcp-server@latest"]
    }
  },
  {
    "name": "GitHub Semantic Search MCP",
    "id": "github-semantic-search",
    "primaryLanguage": "TypeScript",
    "repo": "https://github.com/edelauna/github-semantic-search-mcp",
    "category": ["rag", "search", "github"],
    "mcp": {
      "type": "stdio",
      "command": "node",
      "args": ["/path/to/server-dist.js"]
    }
  },
  {
    "name": "XRAY MCP",
    "id": "xray-mcp",
    "primaryLanguage": "TypeScript",
    "repo": "https://github.com/xray-app/xray-mcp",
    "category": ["code-intel", "ast"],
    "mcp": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "xray-mcp"]
    }
  },
  {
    "name": "mcp-debugger",
    "id": "mcp-debugger",
    "primaryLanguage": "TypeScript",
    "repo": "https://github.com/debugmcpdev/mcp-debugger",
    "category": ["debugging", "dap", "python", "node", "rust"],
    "mcp": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@debugmcp/mcp-debugger"]
    }
  },
  {
    "name": "OTEL MCP Server",
    "id": "otel-mcp-server",
    "primaryLanguage": "TypeScript",
    "repo": "https://github.com/shiftyp/otel-mcp-server",
    "category": ["observability", "opentelemetry", "logging"],
    "mcp": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "otel-mcp-server"]
    }
  },
  {
    "name": "Scout Monitoring MCP",
    "id": "scout-mcp-local",
    "primaryLanguage": "Python",
    "repo": "https://github.com/scoutapp/scout-mcp-local",
    "category": ["apm", "logging", "metrics"],
    "mcp": {
      "type": "stdio",
      "command": "python",
      "args": ["-m", "scout_mcp_local"]
    }
  }
]
```