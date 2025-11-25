using System.Diagnostics;
using System.Text;

namespace Ancplua.Mcp.CoreTools.Utils;

/// <summary>
/// Result of a process execution.
/// </summary>
/// <param name="ExitCode">The process exit code (0 typically indicates success).</param>
/// <param name="StandardOutput">The captured standard output.</param>
/// <param name="StandardError">The captured standard error.</param>
public readonly record struct ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    /// <summary>
    /// Whether the process exited successfully (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;

    /// <summary>
    /// Throws if the process failed.
    /// </summary>
    /// <param name="command">Command description for error message.</param>
    /// <exception cref="InvalidOperationException">Thrown when ExitCode is non-zero.</exception>
    public void ThrowIfFailed(string command)
    {
        if (!Success)
        {
            // Truncate output to avoid leaking excessive data in exceptions
            var truncatedStdErr = TruncateForError(StandardError);
            var truncatedStdOut = TruncateForError(StandardOutput);
            throw new InvalidOperationException(
                $"Command '{command}' failed with exit code {ExitCode}.{Environment.NewLine}{truncatedStdErr}{Environment.NewLine}{truncatedStdOut}");
        }
    }

    private static string TruncateForError(string value, int maxLength = 1000)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value[..maxLength] + $"... [truncated, {value.Length - maxLength} chars omitted]";
    }
}

/// <summary>
/// Provides deadlock-safe process execution with proper cancellation support.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the correct pattern for capturing stdout/stderr from child processes.
/// The streams are read asynchronously and in parallel BEFORE waiting for process exit,
/// preventing deadlocks when stream buffers fill up.
/// </para>
/// <para>
/// <b>Cancellation behavior:</b> When a cancellation token is triggered, the child process
/// is killed to prevent orphan processes. Any partial output captured before cancellation
/// is discarded.
/// </para>
/// <para>
/// See: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput#remarks
/// </para>
/// </remarks>
public static class ProcessRunner
{
    /// <summary>
    /// Executes a process with structured arguments.
    /// </summary>
    /// <param name="executable">The executable to run.</param>
    /// <param name="arguments">The arguments to pass (each element is a separate argument).</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="cancellationToken">Cancellation token. When triggered, the process is killed.</param>
    /// <returns>The process result including exit code and output.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the process fails to start.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public static async Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateStartInfo(executable, arguments, workingDirectory);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{executable}'.");

        // Register cancellation callback to kill process if token is triggered.
        // This prevents orphan processes when cancellation occurs.
        await using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited, ignore
            }
        });

        try
        {
            // CRITICAL: Read both streams asynchronously BEFORE WaitForExitAsync
            // to prevent deadlock when stream buffers fill.
            // Note: We don't pass CancellationToken to ReadToEndAsync because:
            // 1. StreamReader.ReadToEndAsync doesn't truly cancel the underlying read
            // 2. Killing the process (via registration above) will close the streams,
            //    causing ReadToEndAsync to complete naturally
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            // After process exits (or is killed), await the stream tasks.
            // Use ConfigureAwait(false) for library code.
            var stdOut = await stdOutTask.ConfigureAwait(false);
            var stdErr = await stdErrTask.ConfigureAwait(false);

            return new ProcessResult(process.ExitCode, stdOut, stdErr);
        }
        catch (OperationCanceledException)
        {
            // Ensure process is killed on cancellation
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }

            throw;
        }
    }

    /// <summary>
    /// Executes a process using a command string that will be parsed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="RunAsync(string, IReadOnlyList{string}, string?, CancellationToken)"/>
    /// with explicit arguments when possible to avoid parsing issues.
    /// </para>
    /// <para>
    /// <b>SECURITY WARNING:</b> This method accepts arbitrary commands. Ensure input
    /// is from trusted sources to prevent command injection attacks.
    /// </para>
    /// </remarks>
    public static async Task<ProcessResult> RunCommandAsync(
        string command,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var (executable, arguments) = CommandParser.Parse(command);
        return await RunAsync(executable, arguments, workingDirectory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a process and throws if it fails.
    /// </summary>
    /// <param name="executable">The executable to run.</param>
    /// <param name="arguments">The arguments to pass.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard output of the process.</returns>
    /// <exception cref="InvalidOperationException">Thrown when process exits with non-zero code.</exception>
    public static async Task<string> RunAndThrowAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(executable, arguments, workingDirectory, cancellationToken).ConfigureAwait(false);
        result.ThrowIfFailed(FormatCommand(executable, arguments));
        return result.StandardOutput;
    }

    /// <summary>
    /// Formats a command for display in error messages, quoting arguments with spaces.
    /// </summary>
    private static string FormatCommand(string executable, IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
            return executable;

        var formattedArgs = arguments.Select(arg =>
            arg.Contains(' ') || arg.Contains('"') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg);
        return $"{executable} {string.Join(' ', formattedArgs)}";
    }

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
}
