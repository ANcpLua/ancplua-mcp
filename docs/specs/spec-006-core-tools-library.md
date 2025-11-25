# spec-006: Ancplua.Mcp.CoreTools Library

## Title
Core Tools Library Specification

## Status
Accepted

## Context
This document describes the implementation details for the new shared library `Ancplua.Mcp.CoreTools`.

See [ADR-006](../decisions/adr-006-core-tools-consolidation.md) for architectural rationale.

## Specification

### Overview
The CoreTools library provides shared MCP tools for filesystem, git, and CI operations used by both HttpServer and WorkstationServer.

### Requirements

#### R1: Project Structure

```
src/Ancplua.Mcp.CoreTools/
├── Ancplua.Mcp.CoreTools.csproj
├── Utils/
│   └── ProcessRunner.cs
└── Tools/
    ├── FileSystemTools.cs
    ├── GitTools.cs
    └── CiTools.cs
```

**Project File:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Description>Shared MCP tools for filesystem, git, and CI operations</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" />
  </ItemGroup>
</Project>
```

#### R2: ProcessRunner (Deadlock Prevention)

**Critical Requirement:** The `ProcessRunner` class MUST implement the deadlock-safe pattern.

```csharp
namespace Ancplua.Mcp.CoreTools.Utils;

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{executable}'.");

        // CRITICAL: Read both streams asynchronously BEFORE WaitForExitAsync
        // to prevent deadlock when stream buffers fill
        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        return new ProcessResult(process.ExitCode, stdOut, stdErr);
    }
}

public readonly record struct ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    public bool Success => ExitCode == 0;

    public void ThrowIfFailed(string command)
    {
        if (!Success)
        {
            throw new InvalidOperationException(
                $"Command '{command}' failed with exit code {ExitCode}.\n{StandardError}\n{StandardOutput}");
        }
    }
}
```

#### R3: Argument Handling

The primary method (`RunAsync`) accepts `IReadOnlyList<string>` to avoid string splitting issues.

For tools that accept a command string (e.g., `CiTools.RunCommandAsync`), provide a helper that handles basic quoting:

```csharp
public static class CommandParser
{
    /// <summary>
    /// Splits a command string into executable and arguments.
    /// Handles basic quoting (double quotes).
    /// </summary>
    public static (string Executable, string[] Arguments) Parse(string command)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in command)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException("Empty command", nameof(command));
        }

        return (parts[0], parts.Skip(1).ToArray());
    }
}
```

#### R4: Tool Migration

Tools must be migrated with minimal API changes. The MCP tool signatures remain the same.

**FileSystemTools:**
| Tool | Input | Output | Notes |
|------|-------|--------|-------|
| `ReadFileAsync` | `path: string` | `Task<string>` | No process execution |
| `WriteFileAsync` | `path, content` | `Task` | No process execution |
| `ListDirectory` | `path: string` | `IEnumerable<string>` | No process execution |
| `DeleteFile` | `path: string` | `void` | No process execution |
| `CreateDirectory` | `path: string` | `void` | No process execution |
| `FileExists` | `path: string` | `bool` | No process execution |
| `DirectoryExists` | `path: string` | `bool` | No process execution |

**GitTools:**
| Tool | Input | Output | Uses ProcessRunner |
|------|-------|--------|-------------------|
| `GetStatusAsync` | `repositoryPath?` | `Task<string>` | Yes |
| `GetLogAsync` | `repositoryPath?, maxCount` | `Task<string>` | Yes |
| `GetDiffAsync` | `repositoryPath?` | `Task<string>` | Yes |
| `ListBranchesAsync` | `repositoryPath?` | `Task<string>` | Yes |
| `GetCurrentBranchAsync` | `repositoryPath?` | `Task<string>` | Yes |
| `AddAsync` | `files, repositoryPath?` | `Task` | Yes |
| `CommitAsync` | `message, repositoryPath?` | `Task` | Yes |

**CiTools:**
| Tool | Input | Output | Uses ProcessRunner |
|------|-------|--------|-------------------|
| `BuildAsync` | `projectPath?` | `Task<string>` | Yes |
| `RunTestsAsync` | `projectPath?` | `Task<string>` | Yes |
| `RestoreAsync` | `projectPath?` | `Task<string>` | Yes |
| `RunCommandAsync` | `command, workingDirectory?` | `Task<string>` | Yes (via CommandParser) |
| `GetDiagnostics` | None | `string` | No |

#### R5: CancellationToken Propagation

All async methods MUST accept and propagate `CancellationToken`:

```csharp
[McpServerTool]
[Description("Runs tests for the project")]
public static async Task<string> RunTestsAsync(
    [Description("The path to the test project (optional)")] string? projectPath = null,
    CancellationToken cancellationToken = default)
{
    var (workingDir, target) = NormalizeDotnetTarget(projectPath);
    var result = await ProcessRunner.RunAsync(
        "dotnet",
        ["test", target],
        workingDir,
        cancellationToken);
    result.ThrowIfFailed("dotnet test");
    return result.StandardOutput;
}
```

### Implementation Considerations

**Server Updates:**

HttpServer `Program.cs`:
```csharp
using Ancplua.Mcp.CoreTools.Tools;  // Changed from Ancplua.Mcp.HttpServer.Tools
// ...
.WithTools<FileSystemTools>()
.WithTools<GitTools>()
.WithTools<CiTools>()
```

WorkstationServer `Program.cs`:
```csharp
using Ancplua.Mcp.CoreTools.Tools;  // Changed from Ancplua.Mcp.WorkstationServer.Tools
// ...
.WithTools<FileSystemTools>()
.WithTools<GitTools>()
.WithTools<CiTools>()
```

**Dockerfile Updates:**
Both Dockerfiles must copy the new project:
```dockerfile
COPY src/Ancplua.Mcp.CoreTools/Ancplua.Mcp.CoreTools.csproj ./src/Ancplua.Mcp.CoreTools/
# ...
COPY src/Ancplua.Mcp.CoreTools/ ./src/Ancplua.Mcp.CoreTools/
```

### Testing

1. **Unit Tests**: Test ProcessRunner with mock processes
2. **Integration Tests**: Existing server tests should pass unchanged
3. **Deadlock Prevention**: Test with large output that would fill buffer

### Security Considerations

1. **Command Injection**: `RunCommandAsync` accepts arbitrary commands - document this risk
2. **Path Traversal**: FileSystemTools should validate paths are within allowed directories
3. **No Secrets in Output**: ProcessRunner logs should not include sensitive data

### Performance Considerations

1. **Async throughout**: All I/O operations are async
2. **No blocking calls**: ProcessRunner never blocks waiting for streams
3. **Efficient string building**: Use StringBuilder for large outputs

## Dependencies

- `ModelContextProtocol` NuGet package
- .NET 10.0

## Timeline
- 2025-11-25 - Draft created
- 2025-11-25 - Accepted
- 2025-11-25 - Implementation started

## References

- [ADR-006](../decisions/adr-006-core-tools-consolidation.md)
- [.NET Process Class Remarks](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput#remarks)
- WorkstationServer implementation: `src/Ancplua.Mcp.WorkstationServer/Tools/CiTools.cs`
