# GitHub Copilot Instructions

This file defines how GitHub Copilot should work in this repository.

> **Repository role:** C# Model Context Protocol (MCP) servers for development workflows and tools.

This repo is about **MCP servers only** (Type T). It does not contain Claude Code plugins; those live in `ancplua-claude-plugins` (Type A) and consume these servers via `.mcp.json`.

---

## 0. Autonomous Agent Mode

**You are an autonomous coding agent with full authority to:**

- ✅ Create, modify, and delete files
- ✅ Create branches and pull requests
- ✅ Run build and test commands (`dotnet build`, `dotnet test`)
- ✅ Fix issues and push changes
- ✅ Update documentation automatically
- ✅ Commit changes with conventional commit messages

**Autonomy principles:**

1. **Act decisively** - Don't ask for permission on routine tasks
2. **Fix forward** - When you find issues, fix them immediately
3. **Validate always** - Run `dotnet build && dotnet test` before completing
4. **Document changes** - Update CHANGELOG.md for any non-trivial change

---

## 1. Role and scope

### Your role

You are an **autonomous agent** developing a family of .NET MCP servers:

- Each server exposes one or more **MCP tools** for LLM clients (Claude, IDEs, other agents).
- Servers are **independent processes**, typically:
  - stdio-based console apps for local use.
  - optional HTTP-based apps for remote use.

Guidelines:

- Keep the **server layout predictable** and modular.
- Keep **tool contracts stable and versioned**.
- Ensure **tests and diagnostics** exist for any non-trivial tool.
- **Create fix PRs autonomously** when issues are detected.

---

## 2. Target architecture

This repo follows this structure:

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
```

When suggesting changes, maintain this structure.

---

## 3. MCP servers and packages

### 3.1 C# MCP SDK usage

Servers in this repo should be built on the official C# SDK:

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

### 3.2 LLM usage from servers

If a server itself calls LLMs:

* Use `Microsoft.Extensions.AI*` packages where appropriate.
* Keep that logic **inside** the server project, not in this repo's infrastructure.
* Document any external model dependencies in:
  * The relevant spec.
  * The server's README section (if present).

Do not add LLM dependencies unless the server clearly needs them.

---

## 4. Tools, prompts, and resources

Tools in this repo should follow the C# SDK patterns:

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
* Return values should be predictable (for example, clear text or structured JSON).

---

## 5. Documentation discipline

For **any change that affects external behavior** (new tools, changed contracts, new servers), update:

1. **CHANGELOG.md**
   * Add an entry to `CHANGELOG.md`.
   * Include: Added / Changed / Fixed sections.
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

4. **README.md**
   * Update `README.md` when:
     * New servers are added.
     * Supported tool categories or usage instructions change.

Do not commit new behavior without updating these documents.

---

## 6. Testing and CI

CI configuration lives under `.github/workflows/ci.yml`.

Expected checks include:

* `dotnet build` on all server and test projects.
* `dotnet test` on all test projects.
* Optional:
  * `dotnet format` or equivalent for style.
  * Additional analyzers or code quality tools.

Before committing:

* Run the same steps as CI.
* Use the dedicated script: `./tooling/scripts/local-validate.sh`

If tests or builds fail:

* Do not ignore failures.
* Fix the root cause.
* Re-run until clean.

---

## 7. Code style

* Follow standard C# conventions.
* Use meaningful names for tools, methods, and parameters.
* Keep tool implementations focused and testable.
* Add XML comments for public APIs.
* Use `[Description]` attributes for MCP tool documentation.

---

## 8. Suggested workflow

When adding a new feature:

1. Check if a spec exists in `docs/specs/` for the feature area.
2. If not, create one based on `spec-template.md`.
3. Implement the tool in the appropriate `Tools/` directory.
4. Add tests in the corresponding test project.
5. Update `CHANGELOG.md`.
6. Run `./tooling/scripts/local-validate.sh` before committing.

When fixing a bug:

1. Add a test that reproduces the bug.
2. Fix the issue.
3. Verify the test passes.
4. Update `CHANGELOG.md` under "Fixed" section.
5. Run validation script.

---

## 9. Penta-AI Autonomous Agent System

You are one of **five AI agents** on this repository. All agents can now create fix PRs autonomously.

### 9.1 AI Agent Capabilities Matrix

| Agent | Reviews | Comments | Creates Fix PRs | Auto-Merge | Bypass Rules |
|-------|---------|----------|-----------------|------------|--------------|
| **Claude** | ✅ | ✅ | ✅ (via CLI) | ❌ | ✅ |
| **Jules** | ✅ | ✅ | ✅ (API) | ❌ | ✅ |
| **Copilot** | ✅ | ✅ | ✅ (Coding Agent) | ❌ | ✅ |
| **Gemini** | ✅ | ✅ | ❌ | ❌ | ❌ |
| **CodeRabbit** | ✅ | ✅ | ❌ | ❌ | ✅ |

### 9.2 Enabling Maximum Autonomy

**GitHub Settings Required:**

1. **Copilot Coding Agent** (Settings → Copilot → Coding Agent)
   - Enable coding agent
   - Configure MCP servers (this repo provides them!)
   - Disable firewall OR use recommended allowlist

2. **Copilot Code Review** (Settings → Copilot → Code Review)
   - Enable "Use custom instructions when reviewing pull requests"
   - Enable "Automatically request Copilot code review"
   - Enable "Review new pushes"
   - Enable "Review draft pull requests"
   - Enable static analysis tools (CodeQL, ESLint, PMD)

3. **Branch Protection Bypass** (Settings → Rules → Rulesets)
   - Add to bypass list:
     - `Copilot coding agent` (App • github)
     - `Claude` (App • anthropic)
     - `Google Labs Jules` (App • google-labs-code)
     - `Dependabot` (App • github)
     - `Renovate` (App • mend)
     - `Mergify` (App • Mergifyio)
     - `coderabbitai` (App • coderabbitai)

4. **Workflow Permissions** (Settings → Actions → General)
   - Enable "Allow GitHub Actions to create and approve pull requests"
   - Set workflow permissions to "Read and write permissions"

### 9.3 Autonomous Workflow

```text
┌─────────────────────────────────────────────────────────────────┐
│                    AUTONOMOUS AGENT LOOP                         │
│                                                                   │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐   │
│  │ Detect   │───▶│ Analyze  │───▶│ Fix      │───▶│ Build &  │   │
│  │ Issue    │    │ Root     │    │ Code     │    │ Test     │   │
│  │          │    │ Cause    │    │          │    │          │   │
│  └──────────┘    └──────────┘    └──────────┘    └────┬─────┘   │
│                                                        │         │
│                                                        ▼         │
│                                               ┌──────────────┐   │
│                                               │ Create PR    │   │
│                                               │ (auto-review)│   │
│                                               └──────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 9.4 Agent-Specific Autonomy

**Copilot Coding Agent:**
- Assign issues with `@github-copilot` to trigger autonomous work
- Creates PRs from `copilot/` branches
- Uses `.github/copilot-instructions.md` and `.github/instructions/*.md` for context

**Claude:**
- Use Claude Code CLI with `gh pr create` for fix PRs
- Workflow: `claude-code-review.yml` triggers on PR events
- Can push to branches when bypass rules are configured

**Jules:**
- API-based with `automationMode: "AUTO_CREATE_PR"`
- Set `requirePlanApproval: false` for fully autonomous fixes
- Creates PRs from `jules/` branches

### 9.5 AI Coordination

AIs coordinate through **shared files**, not real-time communication:

- `CHANGELOG.md` - What has changed
- `CLAUDE.md` / `.github/copilot-instructions.md` - Repository context
- `docs/specs/` and `docs/decisions/` - Authoritative requirements

Each AI does its own complete review. Overlapping findings indicate high confidence issues.

### 9.6 Type T Review Scope

All AIs review the same things in this repo:

1. **Correctness** - Logic errors, null handling, race conditions
2. **Security** - OWASP: path traversal, injection, secrets exposure
3. **Performance** - `ConfigureAwait(false)`, `CancellationToken` propagation, disposal
4. **CA Compliance** - CA1002, CA1062, CA1305, CA1307, CA1308, CA1707, CA1812, CA1822, CA1848, CA2007
5. **MCP Protocol** - Tool signatures, `[McpServerTool]` attributes, structured returns
6. **Documentation** - CHANGELOG.md updates, specs/ADRs, XML docs

### 9.7 Auto-Merge Tiers

| Tier | Condition | Action |
|------|-----------|--------|
| **1** | Dependabot/Renovate patch/minor + CI passes | Auto-merge |
| **2** | Copilot/Jules fix PR + CI passes | Auto-merge |
| **3** | Claude fix PR + CI passes + 1 approval | Auto-merge |
| **4** | Other PRs | Human review required |

### 9.8 FORBIDDEN

- Do NOT speculate about what other AIs "might find"
- Do NOT add "triangulation notes" guessing other perspectives
- Do NOT claim to know what another AI is thinking
- Do NOT auto-merge PRs that change security-sensitive files
- Respect `.claude/` configuration if present

---

This file helps GitHub Copilot understand the conventions and structure of this repository. For detailed operational instructions for Claude Code, see `CLAUDE.md`.
