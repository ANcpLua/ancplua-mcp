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
dotnet run --project WorkstationServer/WorkstationServer.csproj
```

### Running HttpServer

```bash
dotnet run --project HttpServer/HttpServer.csproj
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
├── WorkstationServer/          # Stdio MCP server
│   ├── Program.cs
│   └── Tools/                  # Tool implementations
│       ├── FileSystemTools.cs
│       ├── GitTools.cs
│       └── CiTools.cs
├── HttpServer/                 # HTTP MCP server
│   ├── Program.cs
│   └── Tools/                  # Tool implementations
├── tests/
│   ├── WorkstationServer.Tests/
│   └── HttpServer.Tests/
├── docs/                       # Documentation
│   ├── ARCHITECTURE.md
│   ├── specs/                  # Specifications
│   ├── decisions/              # Architecture Decision Records (ADRs)
│   └── examples/               # Configuration examples
└── tooling/
    └── scripts/                # Build and validation scripts
```

## Development

### Prerequisites

- .NET 9.0 SDK or later
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
=======
# ancplua-mcp (working name)

**C# MCP servers for development workflows and tools.**

This repository hosts one or more **Model Context Protocol (MCP)** servers implemented in C#.  
They expose local and remote tools (filesystem, repositories, CI, etc.) to LLM clients such as Claude, IDEs, and other agents.

The focus is:

- Clean, minimal MCP server implementations using the official C# SDK.
- Clear separation from Claude Code plugins (which live in another repository).
- Strong documentation and versioning discipline (CHANGELOG, specs, ADRs).

You can rename the repository later; the structure and contracts are designed to survive refactoring.

---

## 1. What this repository is (and is not)

This repo **is**:

- A home for **.NET MCP servers**:
  - stdio-based servers for local use.
  - optionally HTTP-based servers for remote use.
- A place to define **MCP tools** for:
  - interacting with local dev environments.
  - automating common development workflows.
  - exposing curated system capabilities to LLMs.

This repo is **not**:

- A Claude Code plugin marketplace.
  - Plugins and Skills live elsewhere (for example, `ancplua-claude-plugins`).
- A generic monolithic application.
  - Each server is focused and versioned.

Other repositories connect to these servers using `.mcp.json` and MCP-aware clients.

---

## 2. Repository layout (target)

The repository is converging toward:

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
│   └── Ancplua.Mcp.HttpServer/           # Optional HTTP-based MCP server
│       ├── Ancplua.Mcp.HttpServer.csproj
│       ├── Program.cs
│       └── Tools/
│
├── tests/
│   ├── Ancplua.Mcp.WorkstationServer.Tests/
│   └── Ancplua.Mcp.HttpServer.Tests/
│
├── docs/
│   ├── specs/
│   │   ├── spec-template.md
│   │   └── spec-*.md
│   └── decisions/
│       ├── adr-template.md
│       └── adr-*.md
│
└── .github/
    └── workflows/
        ├── ci.yml
        └── dependabot.yml
````

The actual structure may be simpler at the beginning. The important points:

* **`src/`**: one project per MCP server.
* **`tests/`**: corresponding test projects.
* **`docs/specs/`**: feature-level specifications.
* **`docs/decisions/`**: architecture decision records (ADRs).
* **`CLAUDE.md`**: operational spec for Claude Code in this repo.
* **`CHANGELOG.md`**: user-visible history.

---

## 3. Technology choices

This repository uses the official MCP C# SDK:

* `ModelContextProtocol`

  * Hosting and dependency injection.
  * Stdio-based servers via `AddMcpServer().WithStdioServerTransport()`.
  * Attribute-based tools `[McpServerToolType]` / `[McpServerTool]`.
* `ModelContextProtocol.AspNetCore` (optional)

  * HTTP-based servers via `WithHttpTransport` and `MapMcp`.
* `ModelContextProtocol.Core` (optional)

  * For low-level clients or shared abstractions.

LLM client abstractions (optional):

* `Microsoft.Extensions.AI*` packages MAY be used **inside server projects** that need to call LLMs themselves.
* They are not required for basic MCP servers that only expose local tools.

Target framework:

* Intended for modern .NET (for example, .NET 8/9/10).
* Exact `TargetFramework` and package versions are managed in project files and/or shared props.

---

## 4. Servers

### 4.1 Workstation server (stdio)

The **workstation server** is a console application that speaks MCP over stdio.

Typical Program.cs shape:

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
    .WithToolsFromAssembly(); // or WithTools<FileSystemTools>().WithTools<GitTools>() etc.

await builder.Build().RunAsync();
```

Tools live in `Tools/` and are annotated:

```csharp
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class FileSystemTools
{
    [McpServerTool, Description("Lists files in a directory.")]
    public static string[] ListFiles(
        [Description("Path to the directory to list.")]
        string path)
    {
        // Implementation here
        return Array.Empty<string>();
    }
}
```

This server is ideal for **local dev setups** and can be wired into:

* Claude Desktop / Claude Code.
* IDE integrations (for example, Rider MCP support).
* Other MCP-aware clients.

### 4.2 HTTP server (optional)

An **HTTP-based MCP server** can be added in `Ancplua.Mcp.HttpServer` using `ModelContextProtocol.AspNetCore`.

Use this when:

* You want to expose MCP tools over HTTP/S.
* Multiple clients need to connect to a shared server.

The exact layout and routing should be documented in a spec and ADR when introduced.

---

## 5. How to run and connect

### 5.1 Running the workstation server

From the repo root:

```bash
dotnet restore
dotnet build

# Run workstation MCP server (adjust path to match actual project name)
dotnet run --project src/Ancplua.Mcp.WorkstationServer
```

The server will:

* Speak MCP over stdio.
* Log to stderr.

### 5.2 Example `.mcp.json` (client-side)

Client configurations (for example, Claude or IDEs) will typically reside in a **different repo or local config directory**.

Example `.mcp.json` a client might use:

```json
{
  "mcpServers": {
    "ancplua-workstation": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/Ancplua.Mcp.WorkstationServer"
      ],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

This file is not required to live in this repo, but a `docs/specs` entry should describe the expected configuration pattern for each server.

---

## 6. Development workflow

### 6.1 For Claude Code

Claude’s behavior in this repo is defined in `CLAUDE.md` and includes:

* Full local permissions when explicitly launched with the appropriate flags.
* No commits / pushes.
* Obligatory **documentation updates** (CHANGELOG, specs, ADRs) when behavior changes.

### 6.2 For humans

Typical workflow:

1. Clone the repo.

2. Ensure .NET SDK is installed.

3. Run:

   ```bash
   dotnet restore
   dotnet build
   dotnet test
   ```

4. For feature work:

   * Draft or update a spec in `docs/specs/`.
   * If it’s an architectural choice, draft or update an ADR in `docs/decisions/`.

5. Implement tools in `src/` projects.

6. Update `CHANGELOG.md` with user-visible changes.

7. Open PRs that include:

   * Code changes.
   * Updated docs.
   * Passing tests.

---

## 7. Documentation structure

Documentation is split into:

* `CLAUDE.md`

  * Operational spec for Claude Code in this repo.
* `CHANGELOG.md`

  * Versioned history of visible changes.
* `docs/specs/`

  * Feature-level specifications (one per feature or tool-group).
* `docs/decisions/`

  * Architecture Decision Records.

Templates:

* `docs/specs/spec-template.md`
* `docs/decisions/adr-template.md`

When you add a new capability:

1. Create/update a spec.
2. Create/update an ADR if architecture is affected.
3. Update `CHANGELOG.md`.
4. Adjust `README.md` as needed.

---

## 8. Relationship to other repos

This repo is designed to **complement** but not duplicate:

* A Claude Code plugin marketplace (for example, `ancplua-claude-plugins`):

  * That repo defines plugins, Skills, hooks, and `.mcp.json` client configs.
  * This repo defines MCP servers only.

Contracts between the two are:

* Server start commands and environment.
* Tool names and input/output schemas.
* Stability and versioning as recorded in specs and the changelog.

Changes that affect those contracts must be documented clearly so clients can adjust.

---

## License

MIT License - see [LICENSE](LICENSE) file for details.