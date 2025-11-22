# Architecture

This document describes the intended architecture of the **ancplua-mcp** repository.

The goal is to provide **small, focused MCP servers** implemented in C#, with clear contracts and minimal dependencies. This repo is for **servers only**. Claude Code plugins, IDE plugins, and other clients live elsewhere and connect to these servers via `.mcp.json` (or equivalent) configuration.

---

## 1. High-level overview

**Primary responsibilities of this repository:**

- Implement one or more **MCP servers** in .NET.
- Expose **tools** (filesystem, Git, CI, etc.) to LLM clients via the **Model Context Protocol**.
- Maintain **clear contracts** and **strong documentation**:
  - Tool signatures and behavior.
  - Server startup commands.
  - Versioned changes (via `CHANGELOG.md`, specs, ADRs).

**Out of scope for this repository:**

- Claude Code **plugins** and **Skills** (those live in `ancplua-claude-plugins`).
- Frontend applications, web UIs, or IDE plugins.
- Orchestrating which client uses which server (that is handled by client configuration).

---

## 2. Repository layout

Target layout:

```text
ancplua-mcp/
├── README.md
├── CLAUDE.md
├── CHANGELOG.md
├── .gitignore
│
├── src/
│   ├── Ancplua.Mcp.WorkstationServer/
│   │   ├── Ancplua.Mcp.WorkstationServer.csproj
│   │   ├── Program.cs
│   │   └── Tools/
│   │       ├── FileSystemTools.cs
│   │       ├── GitTools.cs
│   │       └── CiTools.cs
│   │
│   └── Ancplua.Mcp.HttpServer/
│       ├── Ancplua.Mcp.HttpServer.csproj
│       ├── Program.cs
│       └── Tools/
│
├── tests/
│   ├── Ancplua.Mcp.WorkstationServer.Tests/
│   └── Ancplua.Mcp.HttpServer.Tests/
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── specs/
│   │   ├── spec-template.md
│   │   └── spec-*.md
│   └── decisions/
│       ├── adr-template.md
│       └── adr-*.md
│
├── docs/examples/
│   ├── claude-workstation.mcp.json
│   ├── claude-http.mcp.json
│   └── rider-workstation.mcp.json
│
├── tooling/
│   └── scripts/
│       └── local-validate.sh
│
└── .github/
    └── workflows/
        ├── ci.yml
        └── dependabot.yml
```

If the actual filesystem differs, move toward this layout incrementally rather than via a single large refactor.

---

## 3. Server architecture

### 3.1 Common building blocks

All servers use the official C# MCP SDK:

* `ModelContextProtocol`

  * `AddMcpServer()`
  * `WithStdioServerTransport()`
  * `WithToolsFromAssembly()` or `WithTools<TToolType>()`
* `ModelContextProtocol.AspNetCore` (HTTP server only)

  * `WithHttpTransport(...)`
  * `MapMcp(...)` on an ASP.NET Core pipeline

Tools are defined via attributes in the `ModelContextProtocol.Server` namespace:

* `[McpServerToolType]` on a static class that groups tools.
* `[McpServerTool]` on individual methods.
* `[Description]` (or XML comments) for tool and parameter documentation.

Optional:

* `[McpServerPromptType]` / `[McpServerPrompt]`
* `[McpServerResourceType]` / `[McpServerResource]`

Each server is responsible for:

* Hosting and transport (stdio or HTTP).
* Registering tool types.
* Wiring logging and DI.

### 3.2 Ancplua.Mcp.WorkstationServer

Purpose:

* **Local, stdio-based MCP server** suitable for:

  * Claude Desktop / Claude Code.
  * IDE integrations (for example, Rider MCP).
* Exposes **workstation-style tools**, e.g.:

  * `FileSystemTools`: list files, read files, basic filesystem queries.
  * `GitTools`: status, diff summaries, branch info.
  * `CiTools`: run local CI checks, inspect last run, etc.

Key characteristics:

* Transport: **stdio**.
* Intended to run **on the same machine** as the client.
* Does not listen on a network port.
* Minimal dependencies (prefer `ModelContextProtocol` only).

Example Program skeleton (conceptual):

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(); // Registers all [McpServerToolType] in this assembly

await builder.Build().RunAsync();
```

### 3.3 Ancplua.Mcp.HttpServer (optional)

Purpose:

* **HTTP-based MCP server** for:

  * Shared, remote, or multi-tenant tool access.
  * More complex deployments behind reverse proxies or API gateways.

Key characteristics:

* Transport: **HTTP**, implemented with `ModelContextProtocol.AspNetCore`.
* Runs as an ASP.NET Core application.
* Can expose different tool sets per route, tenant, or API key (documented in specs/ADRs when added).

This server is optional. It should only be added when there is a clear, documented need (spec + ADR).

---

## 4. Tool design

Tools should follow these principles:

* **Single responsibility**: each tool does one clear thing.
* **Stable names**: tool names MUST be stable once clients rely on them.
* **Explicit parameters**:

  * Simple .NET types where possible (`string`, numeric types, enums, etc.).
  * Clear descriptions for each parameter.
* **Predictable outputs**:

  * Either simple results (strings, arrays) or well-defined DTOs.
  * JSON-serializable without ambiguity.

Tools are grouped by domain:

* `FileSystemTools` – local filesystem operations.
* `GitTools` – version control operations.
* `CiTools` – CI / test workflows.
* Additional groups MAY be added; each new group must be documented in specs and the changelog.

---

## 5. Documentation and versioning

This repository uses:

* `CHANGELOG.md`

  * Records user-visible changes (Added / Changed / Fixed).
  * Each entry references affected servers and tool families.
* `docs/specs/`

  * One spec per feature or tool group.
  * Describes:

    * Problem and value.
    * Tool signatures (inputs/outputs).
    * Example usage.
* `docs/decisions/`

  * Architecture Decision Records (ADRs).
  * Capture structural decisions (e.g. "introduce HTTP server", "split tools into new project").

When implementing or modifying tools/servers:

1. Update or create **spec**.
2. Update or create **ADR** if the architecture changes.
3. Add an entry to **CHANGELOG.md**.
4. Update **README.md** if public usage changes.
5. Adjust **docs/examples/*.mcp.json** if client configuration needs to change.

---

## 6. Client configuration examples

Client configuration files do **not** drive the servers directly; they are examples to help:

* Claude Desktop / Claude Code.
* JetBrains Rider MCP.
* Other MCP clients.

These examples live under `docs/examples/` and are treated as:

* Copy-paste templates for users.
* Documentation of expected startup commands and environment.

They must be kept in sync with:

* Actual project names and paths under `src/`.
* Major breaking changes in server behavior.

---

## 7. Testing and CI

Tests:

* Each server has a corresponding test project under `tests/`.
* Tests cover:

  * Tool behavior.
  * Error paths (invalid parameters, I/O errors, etc.).
  * Integration scenarios where practical.

CI:

* Defined in `.github/workflows/ci.yml`.
* At minimum:

  * `dotnet restore`
  * `dotnet build`
  * `dotnet test`
* May include additional checks (formatting, analyzers, security scans).

Local validation:

* `tooling/scripts/local-validate.sh` runs a subset or all of CI steps for local use.
* Claude Code uses the same script when validating work in this repo.

---

## 8. Relationship to other repositories

This repository is designed to cooperate with, but remain independent from:

* `ancplua-claude-plugins`:

  * Contains Claude Code plugins and Skills.
  * Uses `.mcp.json` to connect to MCP servers defined here.
* Any other client repos (IDE helpers, agent frameworks, etc.) that:

  * Reference these servers via configuration.
  * Rely on documented tool names and contracts.

Contract boundaries:

* Server **startup commands** and configuration (documented here and in specs).
* Tool **names**, **inputs**, and **outputs** (documented in specs and examples).
* **Versioning** of behavior and breaking changes (documented in `CHANGELOG.md` and ADRs).

Any change that affects these boundaries must be documented before being considered complete.
