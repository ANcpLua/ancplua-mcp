# Spec-002: Workstation MCP Tools

**Status**: Implemented
**Last Updated**: 2025-11-22

## Overview

This spec describes the initial batch of MCP tools provided by the Ancplua.Mcp.WorkstationServer for local development workflows.

## Problem

Developers working with MCP-enabled clients (Claude Desktop, IDEs, custom agents) need secure, reliable access to common local operations:

- **Filesystem operations** for reading, writing, and managing project files
- **Git operations** for version control workflows
- **CI/CD operations** for building, testing, and running local commands

These operations must be:
- **Safe**: Prevent destructive operations without clear intent
- **Discoverable**: Well-documented via MCP protocol
- **Composable**: Tools can be combined in workflows

## Solution

Three tool groups exposed via `Ancplua.Mcp.WorkstationServer` using stdio transport:

### FileSystemTools

**Purpose**: Safe filesystem operations within the workspace

**Tools**:
- `ReadFileAsync(path)` - Reads file contents
- `WriteFileAsync(path, content)` - Writes or creates files
- `ListDirectory(path)` - Lists directory contents
- `DeleteFile(path)` - Deletes a file
- `CreateDirectory(path)` - Creates a directory
- `FileExists(path)` - Checks file existence
- `DirectoryExists(path)` - Checks directory existence

**Safety**:
- No automatic directory traversal
- Explicit path required for each operation
- Throws clear exceptions (FileNotFoundException, DirectoryNotFoundException)

### GitTools

**Purpose**: Common git operations for version control

**Tools**:
- `GetStatusAsync(repositoryPath?)` - Gets working tree status
- `GetLogAsync(repositoryPath?, maxCount=10)` - Gets commit history
- `GetDiffAsync(repositoryPath?)` - Gets uncommitted changes
- `ListBranchesAsync(repositoryPath?)` - Lists all branches
- `GetCurrentBranchAsync(repositoryPath?)` - Gets active branch name
- `AddAsync(files, repositoryPath?)` - Stages files
- `CommitAsync(message, repositoryPath?)` - Creates commits

**Safety**:
- All commands use ArgumentList (no shell injection)
- Errors returned as exceptions with git error messages
- No destructive operations (no force push, hard reset, etc.)

### CiTools

**Purpose**: Build, test, and diagnostic operations

**Tools**:
- `BuildAsync(projectPath?)` - Runs dotnet build
- `RunTestsAsync(projectPath?)` - Runs dotnet test
- `RestoreAsync(projectPath?)` - Runs dotnet restore
- `RunCommandAsync(command, workingDirectory?)` - Executes arbitrary commands
- `GetDiagnostics()` - Returns system info

**Safety**:
- `RunCommandAsync` uses simple space-splitting (limitations documented)
- Exit codes and stderr included in output
- Working directory explicitly specified

## Tool Signatures

### Example: ReadFileAsync

```csharp
[McpServerTool]
[Description("Reads the contents of a file at the specified path")]
public static async Task<string> ReadFileAsync(
    [Description("The absolute or relative path to the file")] string path)
```

**Input**:
```json
{
  "path": "/path/to/file.txt"
}
```

**Output**:
```
File contents as string
```

**Errors**:
- `FileNotFoundException` if file doesn't exist

### Example: GetStatusAsync

```csharp
[McpServerTool]
[Description("Gets the status of the git repository")]
public static async Task<string> GetStatusAsync(
    [Description("The path to the git repository (optional)")] string? repositoryPath = null)
```

**Input**:
```json
{
  "repositoryPath": null
}
```

**Output**:
```
M  src/Program.cs
?? new-file.txt
```

**Errors**:
- `InvalidOperationException` if git command fails

## Client Usage

### Claude Desktop Configuration

```json
{
  "mcpServers": {
    "ancplua-workstation": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/ancplua-mcp/src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj"
      ],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Example Tool Calls

**Read a file**:
```
Tool: ReadFileAsync
Input: { "path": "README.md" }
Output: "# ancplua-mcp\n\nC# MCP servers for development workflows..."
```

**Get git status**:
```
Tool: GetStatusAsync
Input: {}
Output: "M  CHANGELOG.md\n?? new-spec.md"
```

**Run tests**:
```
Tool: RunTestsAsync
Input: { "projectPath": "tests/Ancplua.Mcp.WorkstationServer.Tests" }
Output: "Exit Code: 0\nPassed! - Failed: 0, Passed: 5..."
```

## Expected Behavior

### Error Handling

Tools throw typed exceptions that MCP clients can interpret:
- `FileNotFoundException` → File doesn't exist
- `DirectoryNotFoundException` → Directory doesn't exist
- `InvalidOperationException` → Git/command failed with error message

### Working Directories

- FileSystemTools: Paths resolved relative to MCP server working directory
- GitTools: Uses `repositoryPath` if provided, otherwise current directory
- CiTools: Uses `workingDirectory` if provided, otherwise current directory

### Async Operations

All I/O operations are async to prevent blocking the MCP server event loop.

## Future Enhancements

**Not in initial release:**
- File watchers (notify on changes)
- Streaming large file reads
- Git push/pull operations
- More sophisticated command parsing for CiTools

## Testing

Integration tests verify:
- Each tool can be discovered via MCP protocol
- Tool calls return expected results for known inputs
- Errors are properly propagated as MCP error responses

See `tests/Ancplua.Mcp.WorkstationServer.Tests/ToolsTests.cs` for test coverage.

## References

- [MCP C# SDK Documentation](https://github.com/modelcontextprotocol/csharp-sdk)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- CLAUDE.md for operational guidelines
