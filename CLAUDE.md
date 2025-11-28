# CLAUDE.md – ancplua-mcp

> **You are the infrastructure layer.** This repo provides the **Type T (Technology)** tools that power the **Type A (Application)** skills in `ancplua-claude-plugins`.

---

## 0. MANDATORY FIRST ACTIONS

<EXTREMELY_IMPORTANT>

**BEFORE doing anything in this repo:**

1. **Confirm location:**
   ```bash
   pwd
   ls -la *.sln
   ```
   You MUST be at the root of `ancplua-mcp` with `Ancplua.Mcp.sln` visible.

2. **Read CHANGELOG.md:**
   ```
   Read the file: CHANGELOG.md
   ```
   This tells you what has been done recently. Check the `[Unreleased]` section for pending work.

3. **Check for Superpowers:**
   ```bash
   ls ~/.claude/plugins/cache/ 2>/dev/null | grep -i super
   ```
   If installed, read `getting-started/SKILL.md` FIRST.

4. **Load coordination context:**
  - Read this `CLAUDE.md`
  - Read `CHANGELOG.md` (current state of changes)
  - Read `docs/ARCHITECTURE.md`

5. **Understand your role:**
  - You build **tools** (Type T)
  - Skills in `ancplua-claude-plugins` **consume** your tools (Type A)
  - You do NOT write workflows or decision logic

</EXTREMELY_IMPORTANT>

---

## 1. What This Repository Is

> **ancplua-mcp** — The ".NET Insight Spine": C# MCP servers that expose tools for AI-assisted development.

This repo provides:
- **MCP servers** built on the official C# SDK
- **Tools** for filesystem, git, CI, NuGet, Roslyn, architecture
- **Infrastructure** shared via `ServiceDefaults`
- **Integration points** for external "god-tier" MCP servers

This is **NOT**:
- A Claude Code plugin (that's `ancplua-claude-plugins`)
- An IDE extension
- A standalone application

---

## 2. **THE LAW: Specs and Decisions**

<CRITICAL>
**NO TASK IS AUTHORIZED WITHOUT A SPEC AND AN ADR.**
</CRITICAL>

Before writing a single line of code for a new feature or architectural change, you **MUST**:

1.  **Check for an ADR (`docs/decisions/`)**:
    - Why are we doing this?
    - What alternatives were rejected?
    - If no ADR exists, CREATE ONE using `adr-template.md`.

2.  **Check for a Spec (`docs/specs/`)**:
    - What is the interface?
    - What are the inputs/outputs?
    - If no Spec exists, CREATE ONE using `spec-template.md`.

**The Workflow is STRICT:**
`Idea` -> `ADR (Why)` -> `Spec (What)` -> `Implementation (How)`

Any request to "just add a feature" is **REJECTED** until these documents exist.

---

## 3. Target Architecture

```text
ancplua-mcp/
├── CLAUDE.md                    # This file
├── README.md
├── CHANGELOG.md
├── Ancplua.Mcp.sln
│
├── src/
│   ├── Ancplua.Mcp.ServiceDefaults/
│   │   ├── Extensions.cs        # OpenTelemetry, health, resilience
│   │   └── ...
│   │
│   ├── Ancplua.Mcp.WorkstationServer/
│   │   ├── Program.cs           # Stdio MCP server
│   │   └── Tools/
│   │       ├── FileSystemTools.cs
│   │       ├── GitTools.cs
│   │       ├── CiTools.cs
│   │       ├── NuGetTools.cs
│   │       ├── RoslynTools.cs
│   │       ├── RoslynMetricsTools.cs
│   │       └── ArchitectureTools.cs
│   │
│   ├── Ancplua.Mcp.HttpServer/
│   │   ├── Program.cs           # HTTP MCP server
│   │   └── Tools/
│   │
│   ├── Ancplua.Mcp.AIServicesServer/
│   │   ├── Program.cs
│   │   └── Tools/
│   │       └── ServiceDiscoveryTools.cs
│   │
│   └── Ancplua.Mcp.GitHubAppsServer/
│       └── Tools/
│
├── tests/
│   ├── Ancplua.Mcp.WorkstationServer.Tests/
│   ├── Ancplua.Mcp.HttpServer.Tests/
│   └── ...
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── tool-contracts.md        # CRITICAL: Tool name/signature contracts
│   ├── specs/
│   │   └── spec-01XX-*.md       # Specs 0100-0199 reserved for this repo
│   ├── decisions/
│   │   └── ADR-01XX-*.md        # ADRs 0100-0199 reserved for this repo
│   └── examples/
│       └── *.mcp.json           # MCP client config examples
│
└── tooling/
    └── scripts/
        └── local-validate.sh
```

---

## 4. Your Role: Type T Provider

### 4.1 Blood Type Discipline

You are **Type T (Technology Infrastructure)**. You:

| ✅ DO | ❌ DON'T |
|-------|----------|
| Implement tool logic (shell, API, file ops) | Define workflows or decisions |
| Expose typed MCP tools with schemas | Write SKILL.md files |
| Return structured DTOs | Return free-form prose |
| Handle errors and edge cases | Decide business rules |
| Log to stderr, protocol to stdout | Mix concerns |

### 4.2 Tool Design Principles

Every tool you write MUST:

1. **Single responsibility** — One tool, one job
2. **Stable name** — Tool names are public API; changes require spec + ADR
3. **Typed parameters** — Use `[Description]` attributes, explicit types
4. **Structured output** — Return DTOs, not strings
5. **No hidden effects** — If it writes/deletes/calls external API, name says so

```csharp
// GOOD: Clear, typed, documented
[McpServerTool]
[Description("Run dotnet test and return structured results")]
public static async Task<TestResult> RunDotnetTest(
    [Description("Path to project or solution")] string path,
    [Description("Optional test filter expression")] string? filter = null)
{
    // Returns DTO with passed/failed/skipped counts
}

// BAD: Vague, untyped, side effects hidden
[McpServerTool]
public static string DoStuff(string input) { /* ??? */ }
```

---

## 5. Cross-Repo Coordination

### 5.1 The Contract

Skills in `ancplua-claude-plugins` call your tools by name:

```markdown
<!-- In a skill -->
1. Run local tests
   - MCP Tool: `ancplua-workstation.CiTools.RunDotnetTest`
```

Your responsibility:
- Tool `RunDotnetTest` MUST exist
- Signature MUST match documented contract
- Breaking changes MUST be coordinated

### 5.2 Tool Contracts Document

Maintain `docs/tool-contracts.md` with ALL exposed tools:

```markdown
# Tool Contracts

## WorkstationServer Tools

### CiTools.RunDotnetTest
- **Input**: `{ path: string, filter?: string }`
- **Output**: `TestResult { passed: int, failed: int, skipped: int, success: bool }`
- **Side effects**: None (read-only)

### CiTools.WaitForGitHubActions
- **Input**: `{ timeoutMinutes: int }`
- **Output**: `CiStatus { completed: bool, success: bool, url: string }`
- **Side effects**: Polls GitHub API
```

### 5.3 Spec ID Ranges

| Range | Owner |
|-------|-------|
| spec-0001 to spec-0099 | ancplua-claude-plugins |
| **spec-0100 to spec-0199** | **This repo (ancplua-mcp)** |
| spec-0200+ | Cross-repo specs |

Same for ADRs.

### 5.4 Dual-Repo Workflow

Both repos often need synchronized changes (e.g., CI workflows, shared configs).

**Repository Locations:**
```bash
# Type T (Infrastructure) - .NET MCP servers
~/ancplua-mcp

# Type A (Application) - Claude plugins/skills
~/WebstormProjects/ancplua-claude-plugins
```

**Sync Both Repos:**
```bash
# After merging PRs in both repos
cd ~/ancplua-mcp && git fetch origin && git reset --hard origin/main
cd ~/WebstormProjects/ancplua-claude-plugins && git fetch origin && git reset --hard origin/main
```

**Validate Both Repos:**
```bash
# Run validation on both
~/ancplua-mcp/tooling/scripts/local-validate.sh
~/WebstormProjects/ancplua-claude-plugins/tooling/scripts/local-validate.sh

# Or use the --dual flag (validates sibling repo too)
./tooling/scripts/local-validate.sh --dual
```

**Create Matching PRs:**
When changes affect both repos (e.g., workflow updates):
1. Create branch with same name in both repos
2. Make changes in both
3. Create PRs in both
4. Merge both (or enable auto-merge)
5. Sync both repos to main

---

## 6. Mandatory Workflow

### 6.1 For Any Non-Trivial Change

1. **Bootstrap**
   ```bash
   pwd                              # Confirm location
   git status --short               # Check state
   dotnet build                     # Verify builds
   dotnet test                      # Verify tests pass
   ```

2. **Plan & Authorize**
  - **Check Docs/Decisions**: Does an ADR exist? If not, STOP. Create it.
  - **Check Docs/Specs**: Does a Spec exist? If not, STOP. Create it.
  - Only proceed if ADR + Spec are present.

3. **Design (if contract change)**
  - Update spec in `docs/specs/spec-01XX-*.md`
  - Update ADR if architectural decision

4. **Implement**
  - TDD: Write test first
  - Implement tool
  - Document in tool-contracts.md

5. **Validate**
   ```bash
   dotnet restore
   dotnet build --no-restore
   dotnet test --no-build
   ./tooling/scripts/local-validate.sh  # If present
   ```

6. **Document**
  - Update `CHANGELOG.md`
  - Update `docs/tool-contracts.md`
  - Update spec/ADR if needed

7. **Cross-Repo Check**
  - Does any skill reference this tool?
  - If signature changed, update skill docs in ancplua-claude-plugins

---

## 7. Server-Specific Guidelines

### 7.1 WorkstationServer (Stdio)

Primary server for local development. Tools grouped by concern:

| Tool Class | Responsibility |
|------------|----------------|
| `FileSystemTools` | Read/list files, cautious write/delete |
| `GitTools` | Status, branches, diffs (no destructive ops) |
| `CiTools` | dotnet build/test, local CI scripts |
| `NuGetTools` | Package inspection, feed queries |
| `RoslynTools` | Semantic analysis, diagnostics (read-only) |
| `RoslynMetricsTools` | Code metrics, complexity |
| `ArchitectureTools` | Specs, ADRs, diagrams |

### 7.2 HttpServer

Network-accessible version. Can mirror WorkstationServer or expose subset.

Use for:
- Shared team tooling
- CI/CD integration
- Multi-tenant scenarios

### 7.3 AIServicesServer

Orchestrates external AI services (Claude, Gemini, ChatGPT, CodeRabbit, etc.):

| Tool | Purpose |
|------|---------|
| `ServiceDiscoveryTools.ListServices` | Query available AI services |
| `ServiceDiscoveryTools.GetServiceStatus` | Check service health |
| Future: `OrchestrateReview` | Multi-AI PR review |

### 7.4 GitHubAppsServer

Direct GitHub App integration:

| Tool | Purpose |
|------|---------|
| `TriggerAIReview` | Kick off AI review on PR |
| `GetReviewResults` | Fetch completed review |
| `ManageStatusChecks` | Update PR status |

---

## 8. External Server Composition

You compose with external MCP servers, not replace them:

| External Server | Strength | When to Use |
|-----------------|----------|-------------|
| dotnet-build-insights | Binlog analysis | Build failures, perf tuning |
| dotnet-code-insights | Roslyn deep dive | Complex refactors |
| ContextKeeper | Long-term memory | Multi-session context |
| NuGet Context | Package graphs | Dependency issues |
| OTEL MCP | Observability | Runtime debugging |
| mcp-debugger | DAP bridge | Step debugging |
| Jupyter MCP | Python notebooks | Data analysis, ML |

**Rule**: If an external server does it better, use it. Only build tools for ancplua-specific needs.

---

## 9. ServiceDefaults Infrastructure

All servers share `Ancplua.Mcp.ServiceDefaults`:

```csharp
// In any server's Program.cs
builder.AddServiceDefaults();

// Provides:
// - OpenTelemetry (logs, metrics, traces)
// - Health checks (/health, /alive)
// - Resilience (retry, circuit breaker)
// - Stdio discipline (stdout = protocol, stderr = logs)
```

**Rule**: Never add infrastructure code to individual servers. Put it in ServiceDefaults.

---

## 10. Quad-AI Code Review System

This repository uses **automatic quad-AI review** for all PRs. Five AI systems review every PR independently.

### 10.1 AI Tool Capabilities Matrix

| Tool | Reviews | Comments | Creates Fix PRs | Auto-Fix |
|------|---------|----------|-----------------|----------|
| **Claude** | ✅ | ✅ | ❌ | ❌ |
| **Jules** | ✅ | ✅ | ✅ (needs approval) | ❌ |
| **Copilot** | ✅ | ✅ | ❌ | ❌ |
| **Gemini** | ✅ | ✅ | ❌ | ❌ |
| **CodeRabbit** | ✅ | ✅ | ❌ | ❌ |

**The gap:** No AI currently does `detect failure → understand fix → push fix → re-run CI` autonomously.

### 10.2 How They Work Together

```
PR Created/Updated
        │
        ├─► Claude Review (automatic)
        │       └─► Posts formal GitHub review (APPROVE/REQUEST_CHANGES/COMMENT)
        │
        ├─► Jules Review (automatic)
        │       └─► Analyzes code
        │       └─► Creates plan (requires human approval)
        │       └─► If approved: creates fix PR
        │
        ├─► Gemini Review (automatic)
        │       └─► Posts inline suggestions
        │
        ├─► Copilot Review (automatic)
        │       └─► Posts review comments
        │
        └─► CodeRabbit Review (automatic)
                └─► Posts summary + inline comments
```

**Validation Flow:**
1. All five AIs review the same PR independently
2. If multiple AIs flag the same issue → **high confidence**
3. If they disagree → human review needed
4. Jules is the ONLY one that can create fix PRs (with human approval)

### 10.3 Type T Review Scope

All AIs review the same things in this repo:

1. **Correctness** - Logic errors, null handling, race conditions
2. **Security** - OWASP: path traversal, injection, secrets exposure
3. **Performance** - `ConfigureAwait(false)`, `CancellationToken` propagation, disposal
4. **CA Compliance** - CA1002, CA1062, CA1305, CA1307, CA1308, CA1707, CA1812, CA1822, CA1848, CA2007
5. **MCP Protocol** - Tool signatures, `[McpServerTool]` attributes, structured returns
6. **Documentation** - CHANGELOG.md updates, specs/ADRs, XML docs

### 10.4 AI Coordination

AIs coordinate through **shared files**, not real-time communication:
- `CHANGELOG.md` - What has changed
- `CLAUDE.md` / `GEMINI.md` - Repository context
- `docs/specs/` and `docs/decisions/` - Authoritative requirements

Each AI does its own complete review. Overlapping findings indicate high confidence issues.

### 10.5 Configuration Files

| AI | Config Location | Trigger |
|----|-----------------|---------|
| Claude | `.github/workflows/claude-code-review.yml` | `pull_request` |
| Jules | `.github/workflows/jules-auto-review.yml` | `pull_request` |
| Gemini | `.gemini/` directory | Automatic |
| Copilot | `.github/copilot-instructions.md` | Automatic |
| CodeRabbit | `.coderabbit.yaml` (if present) | Automatic |

### 10.6 Skipped PRs

Jules skips: Draft PRs, Dependabot PRs, Renovate PRs

All other AIs run on all non-draft PRs.

---

## 11. Safety & Permissions

You MAY:
- Read/write files in the working directory
- Run `dotnet` commands
- Query external APIs (GitHub, NuGet)
- Execute shell commands documented in tool contracts

You MUST NOT:
- Commit or push (leave for human)
- Hardcode secrets or tokens
- Access files outside repo without explicit env vars
- Break existing tool contracts without spec + ADR

---

## 12. MANDATORY: Update CHANGELOG.md When Done

<CRITICAL>
**BEFORE claiming any task is complete, you MUST update `CHANGELOG.md`.**
</CRITICAL>

After completing ANY task (bug fix, feature, refactor, documentation):

1. Open `CHANGELOG.md`
2. Add entry under `## [Unreleased]` section
3. Categorize as: `### Added`, `### Changed`, `### Fixed`, `### Removed`, `### Security`
4. Include brief description of what was done

**Example entry:**
```markdown
## [Unreleased]

### Fixed
- Fixed deadlock in ProcessRunner by implementing proper async stream reading pattern
- Added path traversal validation to FileSystemTools

### Changed
- GitTools.AddAsync now accepts `IReadOnlyList<string>` instead of single string
```

**This is NOT optional.** A task without a CHANGELOG entry is an incomplete task.

---

## 13. Failure Conditions

You have FAILED if:

- [ ] **Task attempted without existing Spec and ADR**
- [ ] Tool breaks existing contract without spec/ADR
- [ ] Skill in ancplua-claude-plugins can't call your tool
- [ ] Tool returns unstructured string instead of DTO
- [ ] Tests don't pass
- [ ] tool-contracts.md not updated for new/changed tools
- [ ] **CHANGELOG.md not updated** (MANDATORY)
- [ ] Cross-repo impact not checked

---

## 14. Success Conditions

You have SUCCEEDED when:

- [ ] **Spec and ADR exist and are followed**
- [ ] All tests pass
- [ ] tool-contracts.md is accurate
- [ ] New tools have specs
- [ ] Breaking changes have ADRs
- [ ] CHANGELOG updated
- [ ] Skills in ancplua-claude-plugins can consume your tools
- [ ] No hidden side effects

---

## 15. Quick Reference

### Build & Test
```bash
dotnet restore
dotnet build
dotnet test
```

### Run Servers
```bash
# Workstation (stdio)
dotnet run --project src/Ancplua.Mcp.WorkstationServer

# HTTP
dotnet run --project src/Ancplua.Mcp.HttpServer

# AI Services
dotnet run --project src/Ancplua.Mcp.AIServicesServer
```

### Key Files
- `docs/tool-contracts.md` — Tool API reference
- `docs/ARCHITECTURE.md` — System design
- `CHANGELOG.md` — Change history
- `docs/specs/spec-01XX-*.md` — Feature specs
- `docs/decisions/ADR-01XX-*.md` — Architectural decisions

---

**This file is your operational spec. Follow it. Reference CROSS_REPO_COORDINATION.md for integration with ancplua-claude-plugins.**
