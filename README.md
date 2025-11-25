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
dotnet run --project src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj
```

Configure Claude Desktop / Claude Code or other clients to use this as an MCP server.

### 4.3 Run HttpServer (HTTP MCP)

```bash
dotnet run --project src/Ancplua.Mcp.HttpServer/Ancplua.Mcp.HttpServer.csproj
```

Defaults to `https://localhost:5001` / `http://localhost:5000` unless overridden. 

### 4.4 Run AIServicesServer

```bash
dotnet run --project src/Ancplua.Mcp.AIServicesServer/Ancplua.Mcp.AIServicesServer.csproj
```

Or via Docker (if Dockerfiles are present):

```bash
docker build -f Dockerfile.aiservices -t ancplua-ai-services .
docker run -i --rm ancplua-ai-services
```

---

## 5. Example MCP client configuration

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
        "src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj"
      ]
    },
    "ancplua-http": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Ancplua.Mcp.HttpServer/Ancplua.Mcp.HttpServer.csproj"
      ]
    },
    "ancplua-ai-services": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "src/Ancplua.Mcp.AIServicesServer/Ancplua.Mcp.AIServicesServer.csproj"
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
        "src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj"
      ],
      "tools": ["*"]
    },
    "ancplua-ai-services": {
      "type": "local",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/Ancplua.Mcp.AIServicesServer/Ancplua.Mcp.AIServicesServer.csproj"
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

## 6. Machine-friendly MCP server inventory

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
        "src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj"
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
        "src/Ancplua.Mcp.HttpServer/Ancplua.Mcp.HttpServer.csproj"
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
        "src/Ancplua.Mcp.AIServicesServer/Ancplua.Mcp.AIServicesServer.csproj"
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