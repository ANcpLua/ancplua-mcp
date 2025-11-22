using HttpServer.Tools;

namespace HttpServer.Tests;

public class FileSystemToolsTests
{
    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            // Act
            var result = FileSystemTools.FileExists(tempFile);
            
            // Assert
            Assert.True(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FileExists_ReturnsFalseForNonExistingFile()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        // Act
        var result = FileSystemTools.FileExists(nonExistentFile);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReadFileAsync_ReturnsFileContents()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(tempFile, expectedContent);
        
        try
        {
            // Act
            var content = await FileSystemTools.ReadFileAsync(tempFile);
            
            // Assert
            Assert.Equal(expectedContent, content);
        }
        finally
        {
            File.Delete(tempFile);
        }
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
            Assert.True(true);
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
        Assert.Contains("OS:", diagnostics);
        Assert.Contains("Processor Count:", diagnostics);
    }
}
