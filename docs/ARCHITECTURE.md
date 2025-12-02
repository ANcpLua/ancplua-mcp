# Architecture

This document describes the architecture of the **ancplua-mcp** repository and how it fits into a broader “.NET insight spine” composed of multiple MCP servers (both custom and external).   

---

## 1. Goals and non-goals

### 1.1 Goals

- Provide **small, focused MCP servers** implemented in C#/.NET.
- Expose **strongly-typed tools** for:
  - Filesystem, git, CI, NuGet, Roslyn, metrics, architecture docs.
- Act as the **.NET intelligence layer** that can be combined with:
  - MSBuild binlog analysis servers
  - Roslyn analyzer servers
  - NuGet metadata servers
  - Observability servers (OTEL, APM)
- Use **official C# MCP SDK** and a shared **ServiceDefaults** library for consistent infrastructure.   

### 1.2 Non-goals

- Not a client: **no IDE plugins, no Claude skills** here (those live in `ancplua-claude-plugins`).
- Not a monolith: servers do **not** call each other directly.
- Not a replacement for existing “god-tier” MCP servers; this repo is designed to **compose with them**, not to supersede them.

---

## 2. Repository layout

```text
ancplua-mcp/
├── README.md
├── CLAUDE.md
├── CHANGELOG.md
│
├── src/
│   ├── Infrastructure/
│   │   └── ServiceDefaults/           # Shared OpenTelemetry, health, resilience
│   │       ├── Ancplua.Mcp.Infrastructure.ServiceDefaults.csproj
│   │       └── Extensions.cs
│   │
│   ├── Libraries/
│   │   ├── CoreTools/                 # Shared tool implementations
│   │   │   └── Ancplua.Mcp.Libraries.CoreTools.csproj
│   │   └── DebugTools/                # Debug introspection tools
│   │       └── Ancplua.Mcp.Libraries.DebugTools.csproj
│   │
│   └── Servers/
│       ├── Http/
│       │   └── Gateway/               # HTTP MCP server
│       │       └── Ancplua.Mcp.Servers.Http.Gateway.csproj
│       │
│       └── Stdio/
│           ├── AIServices/            # AI service orchestration
│           │   └── Ancplua.Mcp.Servers.Stdio.AIServices.csproj
│           ├── GitHubApps/            # GitHub App integrations
│           │   └── Ancplua.Mcp.Servers.Stdio.GitHubApps.csproj
│           ├── RoslynMetrics/         # Code metrics via Roslyn
│           │   └── Ancplua.Mcp.Servers.Stdio.RoslynMetrics.csproj
│           └── Workstation/           # Local dev tools (filesystem, git, CI)
│               └── Ancplua.Mcp.Servers.Stdio.Workstation.csproj
│
├── tests/
│   ├── Ancplua.Mcp.CoreTools.Tests/
│   ├── Ancplua.Mcp.HttpServer.Tests/
│   ├── Ancplua.Mcp.WorkstationServer.Tests/
│   └── Ancplua.Mcp.Testing/           # Shared test utilities
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── specs/
│   │   ├── spec-template.md
│   │   └── spec-*.md
│   ├── decisions/
│   │   ├── adr-template.md
│   │   └── adr-*.md
│   └── examples/
│       └── *.mcp.json
│
├── tooling/
│   └── scripts/
│       └── local-validate.sh
│
└── .github/
    └── workflows/
        ├── ci.yml
        ├── claude.yml
        ├── claude-code-review.yml
        ├── jules-auto-review.yml
        └── auto-merge.yml
```

**Namespace Convention**: Path = Namespace. `src/Servers/Stdio/Workstation/` → `Ancplua.Mcp.Servers.Stdio.Workstation`. 

---

## 3. Integration with the C# MCP SDK

All servers are built on the **official C# MCP SDK**:

* `ModelContextProtocol`

  * `AddMcpServer()`
  * `WithStdioServerTransport()`
  * `WithToolsFromAssembly()` / `WithTools<TToolType>()`
* `ModelContextProtocol.AspNetCore` (for HTTP servers)

  * `WithHttpTransport()`
  * `MapMcp()` on an ASP.NET Core pipeline 

Tools are defined via attributes in `ModelContextProtocol.Server`:

* `[McpServerToolType]` on static tool groups.
* `[McpServerTool]` on individual methods.
* `[Description]` and XML comments for documentation.

Prompts and resources (optional):

* `[McpServerPromptType]` / `[McpServerPrompt]`
* `[McpServerResourceType]` / `[McpServerResource]`

---

## 4. ServiceDefaults (shared infrastructure)

**Project**: `src/Infrastructure/ServiceDefaults/`

Responsibilities:

* **OpenTelemetry**: logs, metrics, traces.
* **Health checks**: `/health`, `/alive` endpoints for HTTP servers.
* **Resilience**: retry, circuit breaker, timeouts using `Microsoft.Extensions.Http.Resilience`.
* **Service discovery**: for downstream HTTP clients.
* **Stdio logging discipline**: stdout reserved for MCP protocol, logs go to stderr. 

Usage:

```csharp
// Host builder
builder.AddServiceDefaults();

// ASP.NET Core app
app.MapDefaultEndpoints();
```

This keeps **protocol responsibilities** (MCP) separate from **infrastructure**.

---

## 5. Server roles

### 5.1 WorkstationServer (stdio)

Purpose:

* Local, stdio-based MCP server.
* Used by Claude Desktop, Claude Code, Rider, etc., running on the same machine.

Tools:

* `FileSystemTools` – safe read/list operations, with cautious write/delete operations.
* `GitTools` – status, branches, short diffs (no destructive operations by default).
* `CiTools` – local CI tasks (`dotnet build`, `dotnet test`, `./tooling/scripts/local-validate.sh`).
* `NuGetTools` – inspect csproj/package references, feeds, versions.
* `RoslynTools` – semantic validation, diagnostics (read-only).
* `RoslynMetricsTools` – metrics, complex code/architecture smells.
* `ArchitectureTools` – manage specs/ADRs, generate/validate diagrams. 

Key characteristics:

* Transport: **stdio**.
* No TCP ports.
* Minimum dependencies besides MCP SDK + ServiceDefaults.

### 5.2 HttpServer (HTTP MCP)

Purpose:

* Optional; used when you need network-accessible tools (e.g., shared CI agents).
* Exposes tools via HTTP transport (`WithHttpTransport()` + `MapMcp()`).

Use cases:

* Centralized MCP server in a dev cluster.
* Shared workspace tooling for a team.

### 5.3 AIServicesServer

Purpose:

* Orchestrate AI services (Claude, Gemini, ChatGPT, Copilot, CodeRabbit, Codecov).
* Provide a single entry point for **multi-AI workflows**. 

Current state:

* Instruction-based tools that return guidance strings for GitHub Apps, manual execution.

Target state:

* Fully API-integrated tools that call GitHub and other services, with tokens/OAuth.

### 5.4 GitHubAppsServer

Purpose:

* Focused MCP server for GitHub Apps.
* Tools for:

  * Triggering AI reviews.
  * Fetching review results.
  * Managing status checks and coverage.

---

## 6. External servers: “.NET Insight Spine”

Rather than stuffing everything into this repo, you compose with **external MCP servers**:

* Build-time lens: **MSBuild binlogs** (e.g., `dotnet-build-insights`).
* Code-time lens: **Roslyn analyzers** (e.g., `roslyn-mcp`).
* Metadata lens: **NuGet Context MCP**.
* Context lens: **ContextKeeper**.
* Cross-language AST lens: **XRAY**.
* Runtime lens: **OTEL MCP**, **Scout Monitoring MCP**.

The host (Claude / Cursor) composes them:

1. Call `AnalyzeBinlog` → find failing targets/projects.
2. Call your `RoslynTools.ValidateFile` or any Roslyn MCP server on those files.
3. Call `NuGet Context` to inspect package graph if the error is dependency-related.
4. Call OTEL/Scout to correlate with runtime regressions.

No cross-server calls; only **shared DTOs and mental model**.

---

## 7. Tool design principles

* **Single responsibility** – each tool does exactly one thing.
* **Stable names** – tool names become part of the public contract; changes require a spec + ADR.
* **Explicit parameters** – no magic defaults; parameter changes must be backward compatible.
* **JSON-shaped outputs** – avoid free-form strings; make it easy for clients to render tables/graphs.

When evolving tools:

* Write or update a `docs/specs/spec-*.md`.
* Record breaking decisions in `docs/decisions/adr-*.md`.
* Update `CLAUDE.md` if the default agent workflow changes.