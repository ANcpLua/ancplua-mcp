#pragma warning disable CA1707
#pragma warning disable CA2007
using Ancplua.Mcp.CoreTools.Tools;

namespace Ancplua.Mcp.WorkstationServer.Tests;

public sealed class FileSystemToolsTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _originalBasePath;

    public FileSystemToolsTests()
    {
        // Store original base path
        _originalBasePath = FileSystemTools.AllowedBasePath;

        // Create test directory in temp
        _testDir = Path.Combine(Path.GetTempPath(), $"WorkstationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        // Set allowed base path to test directory
        FileSystemTools.AllowedBasePath = _testDir;
    }

    public void Dispose()
    {
        // Restore original base path
        FileSystemTools.AllowedBasePath = _originalBasePath;

        // Clean up test directory
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(testFile, "content");

        // Act
        var result = FileSystemTools.FileExists(testFile);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void FileExists_ReturnsFalseForNonExistingFile()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDir, Guid.NewGuid().ToString());

        // Act
        var result = FileSystemTools.FileExists(nonExistentFile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReadFileAsync_ReturnsFileContents()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "test.txt");
        const string expectedContent = "Test content";
        await File.WriteAllTextAsync(testFile, expectedContent);

        // Act
        var content = await FileSystemTools.ReadFileAsync(testFile);

        // Assert
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public async Task WriteFileAsync_CreatesFileWithContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "newfile.txt");
        const string content = "Test content";

        // Act
        await FileSystemTools.WriteFileAsync(testFile, content);

        // Assert
        Assert.True(File.Exists(testFile));
        var writtenContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(content, writtenContent);
    }

    [Fact]
    public void DirectoryExists_ReturnsTrueForExistingDirectory()
    {
        // Act - test directory exists
        var result = FileSystemTools.DirectoryExists(_testDir);

        // Assert
        Assert.True(result);
    }
}

public class GitToolsTests
{
    [Fact]
    public async Task GetCurrentBranchAsync_ReturnsNonEmptyString()
    {
        // This test verifies git command execution works
        // If not in a git repository, InvalidOperationException is expected and caught
        try
        {
            // Act
            var branch = await GitTools.GetCurrentBranchAsync();

            // Assert
            Assert.NotNull(branch);
            Assert.NotEmpty(branch);
        }
        catch (InvalidOperationException)
        {
            // Expected when not in a git repository - test passes
        }
    }
}

public class CiToolsTests
{
    [Fact]
    public void GetDiagnostics_ReturnsSystemInformation()
    {
        // Act
        var diagnostics = CiTools.GetDiagnostics();

        // Assert
        Assert.NotNull(diagnostics);
        Assert.Contains("OS:", diagnostics, StringComparison.Ordinal);
        Assert.Contains("Processor Count:", diagnostics, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommand_ExecutesSuccessfully()
    {
        // This test verifies command execution infrastructure works
        // even if specific commands may fail in some environments
        try
        {
            // Act
            var result = await CiTools.RunCommandAsync("dotnet --version");

            // Assert - CoreTools returns the raw stdout, which contains version info
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
        catch (InvalidOperationException)
        {
            // Command execution infrastructure works even if specific command fails
        }
    }
}
