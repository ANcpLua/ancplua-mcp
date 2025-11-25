using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.WorkstationServer.Tools;

/// <summary>
/// Provides MCP tools for Git operations including status, log, diff, and branch management.
/// </summary>
[McpServerToolType]
public  class GitTools
{
    private static ProcessStartInfo CreateGitStartInfo(
        IReadOnlyList<string> arguments,
        string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
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
    /// Starts a git process and safely captures stdout + stderr without deadlocks.
    /// </summary>
    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunGitAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateGitStartInfo(arguments, workingDirectory);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git process.");

        // Start reading both streams immediately to avoid deadlocks when both are redirected.
        // See docs & community warnings about sequential ReadToEnd on both streams.
        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        return (process.ExitCode, stdOut, stdErr);
    }

    private static Task<(int ExitCode, string StdOut, string StdErr)> RunGitAsync(
        string arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        // NOTE: this is still a simple splitter; for complex quoting prefer the IList<string> overload.
        var args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return RunGitAsync(args, workingDirectory, cancellationToken);
    }

    private static async Task<string> ExecuteGitCommandAsync(
        string arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var (exitCode, stdOut, stdErr) = await RunGitAsync(arguments, workingDirectory, cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Git command failed with exit code {exitCode}.{Environment.NewLine}{stdErr}");
        }

        return stdOut;
    }

    private static async Task ExecuteGitCommandAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var (exitCode, _, stdErr) = await RunGitAsync(arguments, workingDirectory, cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Git command failed with exit code {exitCode}.{Environment.NewLine}{stdErr}");
        }
    }

    // ---------- MCP tools ----------

    /// <summary>
    /// Gets the status of the git repository.
    /// </summary>
    [McpServerTool]
    [Description("Gets the status of the git repository")]
    public static async Task<string> GetStatusAsync(
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
        }

        return await ExecuteGitCommandAsync("status --porcelain", repositoryPath, cancellationToken);
    }

    /// <summary>
    /// Gets the git log for the repository.
    /// </summary>
    [McpServerTool]
    [Description("Gets the git log for the repository")]
    public static async Task<string> GetLogAsync(
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        [Description("Maximum number of commits to return")]
        int maxCount = 10,
        CancellationToken cancellationToken = default)
    {
        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
        }

        return await ExecuteGitCommandAsync($"log --oneline --max-count={maxCount}", repositoryPath, cancellationToken);
    }

    /// <summary>
    /// Gets the diff for the repository.
    /// </summary>
    [McpServerTool]
    [Description("Gets the diff for the repository")]
    public static async Task<string> GetDiffAsync(
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
        }

        return await ExecuteGitCommandAsync("diff", repositoryPath, cancellationToken);
    }

    /// <summary>
    /// Lists branches in the repository.
    /// </summary>
    [McpServerTool]
    [Description("Lists branches in the repository")]
    public static async Task<string> ListBranchesAsync(
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
        }

        return await ExecuteGitCommandAsync("branch -a", repositoryPath, cancellationToken);
    }

    /// <summary>
    /// Gets the current branch name.
    /// </summary>
    [McpServerTool]
    [Description("Gets the current branch name")]
    public static async Task<string> GetCurrentBranchAsync(
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
        }

        var output = await ExecuteGitCommandAsync("rev-parse --abbrev-ref HEAD", repositoryPath, cancellationToken);
        return output.Trim();
    }

    /// <summary>
    /// Adds files to the staging area.
    /// </summary>
    [McpServerTool]
    [Description("Adds files to the staging area")]
    public static async Task AddAsync(
        [Description("The files to add (e.g., '.' for all files)")]
        string files,
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
        }

        await ExecuteGitCommandAsync(new[] { "add", files }, repositoryPath, cancellationToken);
    }

    /// <summary>
    /// Commits staged changes with a message.
    /// </summary>
    [McpServerTool]
    [Description("Commits staged changes with a message")]
    public static async Task CommitAsync(
        [Description("The commit message")] string message,
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
        }

        await ExecuteGitCommandAsync(new[] { "commit", "-m", message }, repositoryPath, cancellationToken);
    }
}
