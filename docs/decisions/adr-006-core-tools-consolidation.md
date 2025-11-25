# ADR-006: Konsolidierung der Core-Tools und Härtung der Prozessausführung

## Architecture Decision Record

### Title
Core Tools Consolidation and Process Execution Hardening

### Status
Accepted

### Date
2025-11-25

## Context

The project has two primary server implementations: `Ancplua.Mcp.HttpServer` (HTTP transport) and `Ancplua.Mcp.WorkstationServer` (stdio transport). Both servers offer identical "core tools" (`FileSystemTools`, `GitTools`, `CiTools`) that are currently fully duplicated.

This duplication has led to critical issues:

### 1. Maintenance Burden and Divergence
Changes must be made twice, leading to different implementations with varying quality levels.

### 2. Critical Deadlock Risk (HttpServer)
The `HttpServer` implementation uses a flawed sequential reading pattern for `StandardOutput` and `StandardError` when executing external processes:

```csharp
// FLAWED HttpServer pattern (deadlock risk):
var output = await process.StandardOutput.ReadToEndAsync();
var error = await process.StandardError.ReadToEndAsync();
await process.WaitForExitAsync();
```

If either stream buffer fills up, the external process blocks, causing a deadlock.

The `WorkstationServer` implementation uses the correct parallel reading pattern:

```csharp
// CORRECT WorkstationServer pattern:
var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
await process.WaitForExitAsync(cancellationToken);
var stdOut = await stdOutTask;
var stdErr = await stdErrTask;
```

### 3. Flawed Command Parsing
Some helper methods use naive `string.Split(' ')` to parse arguments, which fails for arguments containing spaces or quotes (e.g., `git commit -m "message with spaces"`).

## Decision

We will create a new shared class library: `Ancplua.Mcp.CoreTools`.

1. **Migration**: Duplicated tools will be migrated to this library
2. **ProcessRunner**: A central utility class will be created to encapsulate process execution
3. **Standardization**: Implementation will be standardized on the robust, deadlock-safe pattern (parallel, async reading of StdOut/StdErr)
4. **Argument Handling**: Command-line argument parsing will be improved. Structured argument lists (`IReadOnlyList<string>`) will be preferred to avoid naive string splitting
5. **Server Updates**: `HttpServer` and `WorkstationServer` will be updated to remove local implementations and reference the new library

## Rationale

### Why a shared library?
- **DRY principle**: Single Source of Truth for core tool logic
- **Follows existing patterns**: Matches `ServiceDefaults` and `DebugTools` shared library approach
- **Consistent behavior**: Both servers behave identically
- **Independent evolution**: Core tools can be versioned and tested independently

### Why not just fix HttpServer in place?
- Does not address code duplication
- Risk of implementations diverging again in the future
- Missed opportunity to establish proper patterns

## Consequences

### Positive Consequences
- Elimination of critical deadlock risk in HttpServer
- Reduced duplication (DRY) for core tool logic
- Consistent behavior across both servers
- Improved maintainability
- Better testability (single implementation to test)

### Negative Consequences
- Minor increase in complexity through a new project dependency
- One-time migration effort required
- Lock files will need regeneration

### Neutral Consequences
- No runtime performance impact
- No API changes for MCP clients

## Alternatives Considered

### Alternative 1: Fix HttpServer inline
Fix only the deadlock issue in HttpServer without consolidation.

**Pros:**
- Quick fix for immediate issue

**Cons:**
- Duplication remains
- Will diverge again over time

**Decision:** Rejected - addresses symptom, not root cause

### Alternative 2: Copy WorkstationServer implementation to HttpServer
Replace HttpServer tools with copy of WorkstationServer implementation.

**Pros:**
- Quick to implement

**Cons:**
- Still duplicated code
- Will diverge again

**Decision:** Rejected - same problems persist

## Implementation Notes

1. Create `src/Ancplua.Mcp.CoreTools/` project
2. Implement `Utils/ProcessRunner.cs` with deadlock-safe pattern
3. Migrate tools preserving existing API signatures
4. Update server `Program.cs` to register tools from new namespace
5. Delete local tool implementations from servers
6. Update Dockerfiles to include new project

## Related Decisions

- [ADR-003](adr-003-debug-mcp-tools.md) - Debug MCP Tools (similar shared library pattern)
- [spec-006](../specs/spec-006-core-tools-library.md) - Detailed specification

## References

- [.NET Process deadlock documentation](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput#remarks)
- WorkstationServer robust implementation: `src/Ancplua.Mcp.WorkstationServer/Tools/CiTools.cs`

---

**Template Version**: 1.0
**Last Updated**: 2025-11-25
