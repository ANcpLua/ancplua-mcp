# Spec-002: Workstation MCP Tools

**Status**: Implemented
**Last Updated**: 2025-11-25

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

## Partner Integration: JetBrains Rider

Starting with Rider 2025.3, JetBrains IDEs include a built-in MCP server that exposes ~40 IDE-specific tools. These complement the `Ancplua.Mcp.WorkstationServer` tools by providing deep code intelligence and IDE automation.

### Rider Tool Categories

1.  **Run Configurations**: `get_run_configurations`, `execute_run_configuration`
2.  **Code Analysis**: `get_file_problems`, `get_project_dependencies`, `get_project_modules`
3.  **File Operations**: `create_new_file`, `get_file_text_by_path`, `replace_text_in_file`
4.  **Search & Navigation**: `find_files_by_glob`, `find_files_by_name_keyword`, `search_in_files_by_text`, `search_in_files_by_regex`
5.  **Code Intelligence**: `get_symbol_info`, `rename_refactoring`, `reformat_file`
6.  **Structure**: `list_directory_tree`, `get_all_open_file_paths`
7.  **Terminal**: `execute_terminal_command`
8.  **Version Control**: `get_repositories`

### Integration Pattern

The intended workflow is to run both servers side-by-side in the client configuration:

```json
{
  "mcpServers": {
    "rider": {
      "command": "rider",
      "args": ["mcp", "--project", "/path/to/solution.sln"]
    },
    "ancplua-workstation": {
      "command": "dotnet",
      "args": ["run", "--project", ".../Ancplua.Mcp.WorkstationServer.csproj"]
    }
  }
}
```

This allows the agent to choose the best tool for the job:
*   **Rider**: "Refactor this method", "Run unit tests", "Find usages"
*   **Workstation**: "Commit changes", "Read file (outside project)", "Run CI script"

## References

- [MCP C# SDK Documentation](https://github.com/modelcontextprotocol/csharp-sdk)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- CLAUDE.md for operational guidelines
