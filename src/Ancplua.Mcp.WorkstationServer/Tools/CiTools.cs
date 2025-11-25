using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.WorkstationServer.Tools;

/// <summary>
/// Provides MCP tools for CI/CD operations including running builds, tests, and diagnostics.
/// </summary>
[McpServerToolType]
public class CiTools
{
    private static ProcessStartInfo CreateStartInfo(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory)
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

        return startInfo;
    }

    /// <summary>
    /// Starts a process and safely captures stdout + stderr without deadlocks.
    /// </summary>
    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(executable, arguments, workingDirectory);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{executable}'.");

        // Consume both streams immediately to avoid deadlocks.
        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        return (process.ExitCode, stdOut, stdErr);
    }

    private static async Task<string> ExecuteCommandAsync(
        string executable,
        string[] arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var (exitCode, stdOut, stdErr) = await RunProcessAsync(executable, arguments, workingDirectory, cancellationToken);

        if (exitCode != 0)
        {
            // Preserve stderr + stdout in the exception for debugging.
            throw new InvalidOperationException(
                $"Command '{executable} {string.Join(' ', arguments)}' failed with exit code {exitCode}.{Environment.NewLine}{stdErr}{Environment.NewLine}{stdOut}");
        }

        return stdOut;
    }

    /// <summary>
    /// Simple splitter for ad-hoc commands. For complex quoting, prefer explicit argument arrays.
    /// </summary>
    private static async Task<string> ExecuteCommandAsync(
        string command,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var executable = parts[0];
        var args = parts.Length > 1
            ? parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

        return await ExecuteCommandAsync(executable, args, workingDirectory, cancellationToken);
    }

    /// <summary>
    /// Normalize a project/solution path into (working directory, target argument) for dotnet.
    /// Handles ".", relative paths, directories, and file paths correctly.
    /// </summary>
    private static (string WorkingDirectory, string TargetArgument) NormalizeDotnetTarget(string? projectPath)
    {
        var raw = string.IsNullOrWhiteSpace(projectPath) ? "." : projectPath;
        var fullPath = Path.GetFullPath(raw);

        // If it's a directory, run dotnet *in* that directory with "." as the target.
        if (Directory.Exists(fullPath))
        {
            return (fullPath, ".");
        }

        // Otherwise we assume a file path.
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            // Fallback: no directory info, just use current directory.
            return (Directory.GetCurrentDirectory(), fullPath);
        }

        return (directory, Path.GetFileName(fullPath));
    }

    // ---------- MCP tools ----------

    /// <summary>
    /// Runs the build command for the project.
    /// </summary>
    [McpServerTool]
    [Description("Runs the build command for the project")]
    public static async Task<string> BuildAsync(
        [Description("The path to the project or solution (optional)")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var (workingDir, target) = NormalizeDotnetTarget(projectPath);
        return await ExecuteCommandAsync("dotnet", new[] { "build", target }, workingDir, cancellationToken);
    }

    /// <summary>
    /// Runs tests for the project.
    /// </summary>
    [McpServerTool]
    [Description("Runs tests for the project")]
    public static async Task<string> RunTestsAsync(
        [Description("The path to the test project (optional)")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var (workingDir, target) = NormalizeDotnetTarget(projectPath);
        return await ExecuteCommandAsync("dotnet", new[] { "test", target }, workingDir, cancellationToken);
    }

    /// <summary>
    /// Restores dependencies for the project.
    /// </summary>
    [McpServerTool]
    [Description("Restores dependencies for the project")]
    public static async Task<string> RestoreAsync(
        [Description("The path to the project or solution (optional)")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var (workingDir, target) = NormalizeDotnetTarget(projectPath);
        return await ExecuteCommandAsync("dotnet", new[] { "restore", target }, workingDir, cancellationToken);
    }

    /// <summary>
    /// Runs a custom command in the specified directory.
    /// </summary>
    [McpServerTool]
    [Description("Runs a custom command in the specified directory")]
    public static async Task<string> RunCommandAsync(
        [Description("The command to run")] string command,
        [Description("The working directory (optional)")] string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync(command, workingDirectory, cancellationToken);
    }

    /// <summary>
    /// Gets system diagnostics information.
    /// </summary>
    /// <returns>System diagnostics information.</returns>
    [McpServerTool]
    [Description("Gets system diagnostics information")]
    public static string GetDiagnostics()
    {
        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine($"OS: {Environment.OSVersion}");
        diagnostics.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        diagnostics.AppendLine($"Processor Count: {Environment.ProcessorCount}");
        diagnostics.AppendLine($"Working Directory: {Directory.GetCurrentDirectory()}");
        diagnostics.AppendLine($".NET Version: {Environment.Version}");
        return diagnostics.ToString();
    }
}
