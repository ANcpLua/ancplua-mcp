using System.ComponentModel;
using Ancplua.Mcp.Libraries.CoreTools.Utils;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.Libraries.CoreTools.Tools;

/// <summary>
/// Provides MCP tools for Git operations including status, log, diff, and branch management.
/// </summary>
[McpServerToolType]
[SuppressMessage("Design", "CA1052", Justification = "MCP tool discovery requires non-static types even when members are static.")]
public class GitTools
{
    private static async Task<string> ExecuteGitAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync("git", arguments, workingDirectory, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Git command failed with exit code {result.ExitCode}.{Environment.NewLine}{result.StandardError}");
        }

        return result.StandardOutput;
    }

    private static async Task ExecuteGitNoOutputAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync("git", arguments, workingDirectory, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Git command failed with exit code {result.ExitCode}.{Environment.NewLine}{result.StandardError}");
        }
    }

    private static void ValidateRepositoryPath(string? repositoryPath)
    {
        if (repositoryPath is not null && !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {repositoryPath}");
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
        ValidateRepositoryPath(repositoryPath);
        return await ExecuteGitAsync(["status", "--porcelain"], repositoryPath, cancellationToken)
            .ConfigureAwait(false);
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
        ValidateRepositoryPath(repositoryPath);
        return await ExecuteGitAsync(
            ["log", "--oneline", $"--max-count={maxCount}"],
            repositoryPath,
            cancellationToken).ConfigureAwait(false);
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
        ValidateRepositoryPath(repositoryPath);
        return await ExecuteGitAsync(["diff"], repositoryPath, cancellationToken)
            .ConfigureAwait(false);
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
        ValidateRepositoryPath(repositoryPath);
        return await ExecuteGitAsync(["branch", "-a"], repositoryPath, cancellationToken)
            .ConfigureAwait(false);
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
        ValidateRepositoryPath(repositoryPath);
        var output = await ExecuteGitAsync(
            ["rev-parse", "--abbrev-ref", "HEAD"],
            repositoryPath,
            cancellationToken).ConfigureAwait(false);
        return output.Trim();
    }

    /// <summary>
    /// Adds files to the staging area.
    /// </summary>
    /// <remarks>
    /// Pass multiple files as separate array elements to handle filenames with spaces correctly.
    /// Use "." to add all files.
    /// </remarks>
    [McpServerTool]
    [Description("Adds files to the staging area")]
    public static async Task AddAsync(
        [Description("The files to add (e.g., [\".\"] for all files, or [\"file1.txt\", \"file2.txt\"])")]
        IReadOnlyList<string> files,
        [Description("The path to the git repository (optional)")]
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            throw new ArgumentException("At least one file must be specified", nameof(files));
        }

        ValidateRepositoryPath(repositoryPath);

        // Build arguments: ["add", "--", file1, file2, ...]
        // Using "--" prevents filenames starting with "-" being treated as options
        var args = new List<string>(files.Count + 2) { "add", "--" };
        args.AddRange(files);

        await ExecuteGitNoOutputAsync(args, repositoryPath, cancellationToken).ConfigureAwait(false);
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
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Commit message cannot be empty or whitespace", nameof(message));
        }

        ValidateRepositoryPath(repositoryPath);
        await ExecuteGitNoOutputAsync(["commit", "-m", message], repositoryPath, cancellationToken)
            .ConfigureAwait(false);
    }
}
