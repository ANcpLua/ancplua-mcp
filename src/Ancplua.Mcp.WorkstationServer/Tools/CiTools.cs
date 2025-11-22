using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace Ancplua.Mcp.WorkstationServer.Tools;

/// <summary>
/// Provides MCP tools for CI/CD operations including running builds, tests, and diagnostics.
/// </summary>
[McpServerToolType]
public static class CiTools
{
    /// <summary>
    /// Executes a command with arguments and returns the output.
    /// </summary>
    /// <param name="executable">The executable to run.</param>
    /// <param name="arguments">The arguments as an array.</param>
    /// <param name="workingDirectory">The working directory for the command.</param>
    /// <returns>The output of the command.</returns>
    private static async Task<string> ExecuteCommandAsync(string executable, string[] arguments, string? workingDirectory = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executable,
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
            throw new InvalidOperationException("Failed to start process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
        return $"Exit Code: {process.ExitCode}\n{result}";
    }

    /// <summary>
    /// Executes a shell command and returns the output.
    /// Note: This method splits the command on spaces which may not handle quoted arguments correctly.
    /// For production use, prefer calling BuildAsync, RunTestsAsync, etc. directly.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="workingDirectory">The working directory for the command.</param>
    /// <returns>The output of the command.</returns>
    private static async Task<string> ExecuteCommandAsync(string command, string? workingDirectory = null)
    {
        // Parse command into executable and arguments
        var parts = command.Split(' ', 2);
        var executable = parts[0];
        var arguments = parts.Length > 1 ? parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();

        return await ExecuteCommandAsync(executable, arguments, workingDirectory);
    }

    /// <summary>
    /// Runs the build command for the project.
    /// </summary>
    /// <param name="projectPath">The path to the project or solution.</param>
    /// <returns>The build output.</returns>
    public static async Task<string> BuildAsync(string? projectPath = null)
    {
        var path = projectPath ?? ".";
        return await ExecuteCommandAsync("dotnet", new[] { "build", path }, Path.GetDirectoryName(path));
    }

    /// <summary>
    /// Runs tests for the project.
    /// </summary>
    /// <param name="projectPath">The path to the test project.</param>
    /// <returns>The test output.</returns>
    public static async Task<string> RunTestsAsync(string? projectPath = null)
    {
        var path = projectPath ?? ".";
        return await ExecuteCommandAsync("dotnet", new[] { "test", path }, Path.GetDirectoryName(path));
    }

    /// <summary>
    /// Restores dependencies for the project.
    /// </summary>
    /// <param name="projectPath">The path to the project or solution.</param>
    /// <returns>The restore output.</returns>
    public static async Task<string> RestoreAsync(string? projectPath = null)
    {
        var path = projectPath ?? ".";
        return await ExecuteCommandAsync("dotnet", new[] { "restore", path }, Path.GetDirectoryName(path));
    }

    /// <summary>
    /// Runs a custom command in the specified directory.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>The command output.</returns>
    public static async Task<string> RunCommandAsync(string command, string? workingDirectory = null)
    {
        return await ExecuteCommandAsync(command, workingDirectory);
    }

    /// <summary>
    /// Gets system diagnostics information.
    /// </summary>
    /// <returns>System diagnostics information.</returns>
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
