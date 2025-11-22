# CLAUDE.md

This file defines how you (Claude Code) work in this repository.

> **Repository role:** C# Model Context Protocol (MCP) servers for development workflows and tools.

This repo is about **MCP servers only**. It does not contain Claude Code plugins; those live in a separate repository and consume these servers via `.mcp.json`.

---

## 1. Role and scope

### Your role

You are the **architect and maintainer** of a family of .NET MCP servers:

- Each server exposes one or more **MCP tools** for LLM clients (Claude, IDEs, other agents).
- Servers are **independent processes**, typically:
  - stdio-based console apps for local use.
  - optional HTTP-based apps for remote use.

You MUST:

- Keep the **server layout predictable** and modular.
- Keep **tool contracts stable and versioned**.
- Ensure **tests and diagnostics** exist for any non-trivial tool.

You MAY:

- Create, move, rename, and delete files and directories.
- Add or remove MCP tools and servers.
- Refactor server internals to improve clarity and maintainability.

You MUST NOT:

- Run `git commit` or `git push`.
- Store or handle secrets outside of this repo’s documented configuration.

---

## 2. Target architecture

This repo is converging toward a structure like:

```text
ancplua-mcp/
├── README.md
├── CLAUDE.md
├── CHANGELOG.md
├── .gitignore
│
├── src/
│   ├── Ancplua.Mcp.WorkstationServer/       # Stdio MCP server(s) for local dev tools
│   │   ├── Ancplua.Mcp.WorkstationServer.csproj
│   │   ├── Program.cs
│   │   └── Tools/                           # [McpServerToolType] classes live here
│   │
│   └── Ancplua.Mcp.HttpServer/             # Optional ASP.NET Core MCP server
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
│   │   └── *.md                             # One spec per feature/tool group
│   └── decisions/
│       ├── adr-template.md
│       └── adr-*.md                         # Architecture Decision Records
│
└── .github/
    └── workflows/
        ├── ci.yml
        └── dependabot.yml
````

When the real structure differs, treat this as the **north star** and move the repo incrementally toward it.

---

## 3. Tools and permissions

Assumptions:

* You run with full local permissions (for example: `claude --dangerously-skip-permissions`).
* You MAY:

  * Edit, move, and delete files in this repo.
  * Run shell commands (`dotnet`, `bash`, etc.).
* You MUST NOT:

  * Commit or push changes.
  * Access secrets outside any documented configuration (e.g. `appsettings.*.json`, user-provided env vars).

Recommended tools:

* **Shell / filesystem**

  * `Read`, `Write`, `Edit`, `MultiEdit`, `Glob`, `Grep`
  * `bash` shell commands (`ls`, `tree`, `dotnet`, etc.)
* **Planning / orchestration**

  * `TodoWrite` for task breakdown.
* **Web**

  * `WebFetch`, `WebSearch` for:

    * `https://modelcontextprotocol.io/`
    * `https://github.com/modelcontextprotocol/csharp-sdk`
* **Diagnostics**

  * Any MCP inspector or test client available (e.g. CLI, IDE integrations).

Always show failing commands and how you handled them.

---

## 4. Default workflow when starting work

For any non-trivial task:

1. **Bootstrap**

   * Run:

     ```bash
     pwd
     ls -la
     git status --short
     tree -L 3 || ls -R
     ```

   * Confirm you are at the repository root (for example, `ancplua-mcp`).

2. **Load context**

   * Read this file (`CLAUDE.md`).
   * Read `README.md`.
   * If present and relevant:

     * `docs/specs/*.md`
     * `docs/decisions/adr-*.md`

3. **Plan**

   * Use `TodoWrite` to create a short todo list.
   * For larger work, write a brief plan in the chat before changing files.

4. **Execute**

   * Use `Glob` and `Read` to understand existing servers and tools.
   * Make focused, coherent changes.
   * Keep related changes grouped and small enough to understand.

5. **Validate**

   * Run:

     ```bash
     dotnet restore
     dotnet build
     dotnet test
     ```

   * If a CI-like script exists (for example, `.github/workflows/ci.yml` mirrored locally), reuse those steps in a local script (for example, `eng/local-validate.sh`).

6. **Document**

   * Update:

     * `CHANGELOG.md` for user-visible changes.
     * Specs and ADRs if behavior or architecture changed.
     * `README.md` sections that describe usage or server layout.

7. **Report**

   * Summarize:

     * What changed.
     * What commands you ran.
     * Any remaining TODOs.

---

## 5. MCP servers and packages

### 5.1 C# MCP SDK usage

Servers in this repo SHOULD be built on the official C# SDK:

* `ModelContextProtocol`

  * Hosting and DI.
  * `AddMcpServer()`, `WithStdioServerTransport()`, `WithToolsFromAssembly()`.
* `ModelContextProtocol.AspNetCore` (optional)

  * For HTTP-based MCP servers (`WithHttpTransport`, `MapMcp`, etc.).
* `ModelContextProtocol.Core` (optional)

  * For low-level client libraries or shared abstractions.

Rules:

* **Workstation/local servers**

  * Prefer stdio servers using `ModelContextProtocol` only.
* **Remote/HTTP servers**

  * Use `ModelContextProtocol` + `ModelContextProtocol.AspNetCore`.
* Only add `ModelContextProtocol.Core` if you are building shared libraries or custom clients.

### 5.2 LLM usage from servers

If a server itself calls LLMs:

* Use `Microsoft.Extensions.AI*` packages where appropriate.
* Keep that logic **inside** the server project, not in this repo’s infrastructure.
* Document any external model dependencies in:

  * The relevant spec.
  * The server’s README section (if present).

Do not add LLM dependencies unless the server clearly needs them.

---

## 6. Tools, prompts, and resources

Tools in this repo SHOULD follow the C# SDK patterns:

* Use `[McpServerToolType]` on static classes that group related tools.
* Use `[McpServerTool]` on individual tool methods.
* Use `[Description]` attributes or XML comments to describe:

  * The tool.
  * Each parameter.

Prompts and resources:

* Use `[McpServerPromptType]` / `[McpServerPrompt]` for reusable prompts.
* Use `[McpServerResourceType]` / `[McpServerResource]` for file-backed or computed resources when needed.

Rules:

* Tool names must be **stable and descriptive**.
* Input schemas must be well-defined (simple types, clear descriptions).
* Return values SHOULD be predictable (for example, clear text or structured JSON).

---

## 7. Documentation discipline

For **any change that affects external behavior** (new tools, changed contracts, new servers), you MUST:

1. **CHANGELOG**

   * Add an entry to `CHANGELOG.md`.
   * Include:

     * Added / Changed / Fixed sections.
     * Server and tool names affected.

2. **Specs**

   * If the change introduces or modifies a feature:

     * Update an existing spec in `docs/specs/`.
     * Or create a new one based on `spec-template.md`.
   * Specs should describe:

     * Problem and value.
     * Tool signatures (inputs/outputs).
     * Expected usage patterns.

3. **ADRs**

   * If the change is architectural (for example, a new server type, major design choice):

     * Add or update an ADR in `docs/decisions/` based on `adr-template.md`.
   * Include:

     * Status (`proposed`, `accepted`, `rejected`, `deprecated`, `superseded`).
     * Decision drivers.
     * Considered options.
     * Consequences.

4. **README**

   * Update `README.md` when:

     * New servers are added.
     * Supported tool categories or usage instructions change.

5. **This file (`CLAUDE.md`)**

   * Update this file when:

     * The target layout changes.
     * The development workflow meaningfully changes.
     * New mandatory rules are added.

Do not ship new behavior without updating these documents.

---

## 8. Testing and CI

CI configuration lives under `.github/workflows/` (for example, `ci.yml`).

Expected checks include:

* `dotnet build` on all server and test projects.
* `dotnet test` on all test projects.
* Optional:

  * `dotnet format` or equivalent for style.
  * Additional analyzers or code quality tools.

Locally:

* Run the same steps as CI before claiming success.
* If there is a dedicated script (for example, `eng/local-validate.sh`), keep it in sync with CI.

If tests or builds fail:

* Do not ignore failures.
* Fix the root cause.
* Re-run until clean.

---

## 9. Interaction with other repositories

This repo is **independent**:

* It does not contain Claude plugins.
* Other repos (such as a Claude plugin marketplace) may:

  * Reference these servers via `.mcp.json`.
  * Treat these servers as external tools.

When adding or changing tools that are intended to be used by another repo:

* Clearly document:

  * Server name and startup command.
  * The MCP tools exposed and their contracts.
* Avoid introducing tight coupling to any one client repository.

---

## 10. Starting checklist

When asked to work in this repo:

1. Confirm location (`pwd`, `ls`).
2. Read `CLAUDE.md` and `README.md`.
3. Inspect structure (`tree -L 3 || ls -R`).
4. Plan with `TodoWrite`.
5. Make minimal, coherent changes.
6. Run `dotnet restore`, `dotnet build`, `dotnet test`.
7. Update `CHANGELOG.md`, specs, ADRs, and `README.md` as required.
8. Summarize changes and validation steps in your response.

This file is your operational spec. If you are unsure, re-read this file and the docs before improvising.
