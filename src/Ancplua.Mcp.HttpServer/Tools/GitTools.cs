using System.Diagnostics;
using System.Text;

namespace HttpServer.Tools;

/// <summary>
/// Provides MCP tools for Git operations including status, log, diff, and branch management.
/// </summary>
public class GitTools
{
    /// <summary>
    /// Executes a git command and returns the output.
    /// </summary>
    /// <param name="arguments">The git command arguments as a list.</param>
    /// <param name="workingDirectory">The working directory for the git command.</param>
    /// <returns>The output of the git command.</returns>
    private static async Task<string> ExecuteGitCommandAsync(IEnumerable<string> arguments, string? workingDirectory = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        foreach (var arg in arguments)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }

    /// <summary>
    /// Executes a git command with string arguments and returns the output.
    /// </summary>
    /// <param name="arguments">The git command arguments as a string.</param>
    /// <param name="workingDirectory">The working directory for the git command.</param>
    /// <returns>The output of the git command.</returns>
    private static async Task<string> ExecuteGitCommandAsync(string arguments, string? workingDirectory = null)
    {
        return await ExecuteGitCommandAsync(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries), workingDirectory);
    }

    /// <summary>
    /// Gets the status of the git repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>The git status output.</returns>
    public static async Task<string> GetStatusAsync(string? repositoryPath = null)
    {
        return await ExecuteGitCommandAsync("status --porcelain", repositoryPath);
    }

    /// <summary>
    /// Gets the git log for the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="maxCount">Maximum number of commits to return.</param>
    /// <returns>The git log output.</returns>
    public static async Task<string> GetLogAsync(string? repositoryPath = null, int maxCount = 10)
    {
        return await ExecuteGitCommandAsync($"log --oneline --max-count={maxCount}", repositoryPath);
    }

    /// <summary>
    /// Gets the diff for the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>The git diff output.</returns>
    public static async Task<string> GetDiffAsync(string? repositoryPath = null)
    {
        return await ExecuteGitCommandAsync("diff", repositoryPath);
    }

    /// <summary>
    /// Lists branches in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>The list of branches.</returns>
    public static async Task<string> ListBranchesAsync(string? repositoryPath = null)
    {
        return await ExecuteGitCommandAsync("branch -a", repositoryPath);
    }

    /// <summary>
    /// Gets the current branch name.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>The current branch name.</returns>
    public static async Task<string> GetCurrentBranchAsync(string? repositoryPath = null)
    {
        var output = await ExecuteGitCommandAsync("rev-parse --abbrev-ref HEAD", repositoryPath);
        return output.Trim();
    }

    /// <summary>
    /// Adds files to the staging area.
    /// </summary>
    /// <param name="files">The files to add (e.g., "." for all files).</param>
    /// <param name="repositoryPath">The path to the git repository.</param>
    public static async Task AddAsync(string files, string? repositoryPath = null)
    {
        await ExecuteGitCommandAsync(new[] { "add", files }, repositoryPath);
    }

    /// <summary>
    /// Commits staged changes with a message.
    /// </summary>
    /// <param name="message">The commit message.</param>
    /// <param name="repositoryPath">The path to the git repository.</param>
    public static async Task CommitAsync(string message, string? repositoryPath = null)
    {
        await ExecuteGitCommandAsync(new[] { "commit", "-m", message }, repositoryPath);
    }
}
