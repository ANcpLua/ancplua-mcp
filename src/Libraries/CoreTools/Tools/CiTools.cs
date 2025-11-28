using System.ComponentModel;
using System.Text;
using Ancplua.Mcp.Libraries.CoreTools.Utils;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;
using System.Globalization;

namespace Ancplua.Mcp.Libraries.CoreTools.Tools;

/// <summary>
/// Provides MCP tools for CI/CD operations including running builds, tests, and diagnostics.
/// </summary>
[McpServerToolType]
[SuppressMessage("Design", "CA1052", Justification = "MCP tool discovery requires non-static types even when members are static.")]
public class CiTools
{
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
        return await ProcessRunner.RunAndThrowAsync(
            "dotnet",
            ["build", target],
            workingDir,
            cancellationToken).ConfigureAwait(false);
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
        return await ProcessRunner.RunAndThrowAsync(
            "dotnet",
            ["test", target],
            workingDir,
            cancellationToken).ConfigureAwait(false);
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
        return await ProcessRunner.RunAndThrowAsync(
            "dotnet",
            ["restore", target],
            workingDir,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a custom command in the specified directory.
    /// </summary>
    /// <remarks>
    /// This tool accepts arbitrary shell commands. Use with caution.
    /// </remarks>
    [McpServerTool]
    [Description("Runs a custom command in the specified directory")]
    public static async Task<string> RunCommandAsync(
        [Description("The command to run")] string command,
        [Description("The working directory (optional)")] string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ProcessRunner.RunCommandAsync(command, workingDirectory, cancellationToken)
            .ConfigureAwait(false);
        result.ThrowIfFailed(command);
        return result.StandardOutput;
    }

    /// <summary>
    /// Gets system diagnostics information.
    /// </summary>
    [McpServerTool]
    [Description("Gets system diagnostics information")]
    public static string GetDiagnostics()
    {
        var diagnostics = new StringBuilder();
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}");
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"64-bit OS: {Environment.Is64BitOperatingSystem}");
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {Environment.ProcessorCount}");
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $"Working Directory: {Directory.GetCurrentDirectory()}");
        diagnostics.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");
        return diagnostics.ToString();
    }
}
