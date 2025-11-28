#pragma warning disable CA1707
#pragma warning disable CA2007
using Ancplua.Mcp.Libraries.CoreTools.Utils;

namespace Ancplua.Mcp.CoreTools.Tests;

public class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_SimpleCommand_ReturnsOutput()
    {
        // Arrange & Act
        var result = await ProcessRunner.RunAsync("echo", ["hello"]);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_FailingCommand_ReturnsFailed()
    {
        // Arrange & Act
        var result = await ProcessRunner.RunAsync("false", []);

        // Assert
        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_WithCancellation_KillsProcess()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            // sleep command that would run for 10 seconds
            await ProcessRunner.RunAsync("sleep", ["10"], cancellationToken: cts.Token);
        });
    }

    [Fact]
    public async Task RunAsync_LargeOutput_DoesNotDeadlock()
    {
        // This test verifies the deadlock-safe pattern works.
        // Generate output larger than typical pipe buffer (usually 64KB)
        // Using 'yes' command limited to a certain count, or generate via script

        // Arrange
        // On macOS/Linux, use printf in a loop or similar
        var script = "for i in $(seq 1 10000); do echo \"Line $i with some padding to make it longer and fill the buffer faster\"; done";

        // Act
        var result = await ProcessRunner.RunAsync("sh", ["-c", script]);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Line 10000", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_LargeStderr_DoesNotDeadlock()
    {
        // Test large stderr output doesn't deadlock
        var script = "for i in $(seq 1 10000); do echo \"Error line $i\" >&2; done";

        // Act
        var result = await ProcessRunner.RunAsync("sh", ["-c", script]);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Error line 10000", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_BothOutputsFilled_DoesNotDeadlock()
    {
        // Test both stdout and stderr large output simultaneously
        var script = @"
for i in $(seq 1 5000); do
    echo ""stdout line $i""
    echo ""stderr line $i"" >&2
done
";

        // Act
        var result = await ProcessRunner.RunAsync("sh", ["-c", script]);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("stdout line 5000", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("stderr line 5000", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAndThrowAsync_Success_ReturnsOutput()
    {
        // Act
        var output = await ProcessRunner.RunAndThrowAsync("echo", ["test"]);

        // Assert
        Assert.Contains("test", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAndThrowAsync_Failure_ThrowsWithTruncatedOutput()
    {
        // Arrange - command that fails with output
        var script = "echo 'Error message'; exit 1";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ProcessRunner.RunAndThrowAsync("sh", ["-c", script]);
        });

        Assert.Contains("failed with exit code 1", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommandAsync_ParsesAndExecutes()
    {
        // Act
        var result = await ProcessRunner.RunCommandAsync("echo hello world");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("hello", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_ArgumentsWithSpaces_HandledCorrectly()
    {
        // Arrange - echo with a single argument containing spaces
        // Since we use ArgumentList, this should be properly handled

        // Act
        var result = await ProcessRunner.RunAsync("echo", ["hello world with spaces"])
            ;

        // Assert
        Assert.True(result.Success);
        Assert.Contains("hello world with spaces", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessResult_ThrowIfFailed_TruncatesLongOutput()
    {
        // Arrange
        var longOutput = new string('x', 2000);
        var result = new ProcessResult(1, longOutput, "error");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => result.ThrowIfFailed("test"));

        // Verify truncation occurred
        Assert.Contains("truncated", ex.Message, StringComparison.Ordinal);
        Assert.True(ex.Message.Length < longOutput.Length + 500); // Some overhead for other text
    }
}
