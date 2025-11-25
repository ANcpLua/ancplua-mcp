#pragma warning disable CA1707
using Ancplua.Mcp.CoreTools.Tools;

namespace Ancplua.Mcp.CoreTools.Tests;

public sealed class FileSystemToolsTests : IDisposable
{
    private readonly string _testDir;

    public FileSystemToolsTests()
    {
        // Create a unique test directory for each test run
        _testDir = Path.Combine(Path.GetTempPath(), $"CoreToolsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        // Set the allowed base path to our test directory
        FileSystemTools.AllowedBasePath = _testDir;
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ReadFileAsync_ExistingFile_ReturnsContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "Hello, World!");

        // Act
        var content = await FileSystemTools.ReadFileAsync(filePath);

        // Assert
        Assert.Equal("Hello, World!", content);
    }

    [Fact]
    public async Task ReadFileAsync_NonExistingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "nonexistent.txt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            FileSystemTools.ReadFileAsync(filePath));
    }

    [Fact]
    public async Task ReadFileAsync_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange - try to read outside allowed directory
        var outsidePath = Path.Combine(_testDir, "..", "outside.txt");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            FileSystemTools.ReadFileAsync(outsidePath));
    }

    [Fact]
    public async Task ReadFileAsync_AbsolutePathOutside_ThrowsUnauthorizedAccessException()
    {
        // Arrange - absolute path outside allowed directory
        var outsidePath = "/etc/passwd";

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            FileSystemTools.ReadFileAsync(outsidePath));
    }

    [Fact]
    public async Task WriteFileAsync_NewFile_CreatesFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "newfile.txt");

        // Act
        await FileSystemTools.WriteFileAsync(filePath, "Test content");

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal("Test content", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task WriteFileAsync_CreatesParentDirectories()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "subdir", "nested", "file.txt");

        // Act
        await FileSystemTools.WriteFileAsync(filePath, "Nested content");

        // Assert
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task WriteFileAsync_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var outsidePath = Path.Combine(_testDir, "..", "malicious.txt");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            FileSystemTools.WriteFileAsync(outsidePath, "malicious content"));
    }

    [Fact]
    public void ListDirectory_ExistingDirectory_ReturnsEntries()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "file1.txt"), "");
        File.WriteAllText(Path.Combine(_testDir, "file2.txt"), "");
        Directory.CreateDirectory(Path.Combine(_testDir, "subdir"));

        // Act
        var entries = FileSystemTools.ListDirectory(_testDir).ToList();

        // Assert
        Assert.Contains("file1.txt", entries);
        Assert.Contains("file2.txt", entries);
        Assert.Contains("subdir", entries);
    }

    [Fact]
    public void ListDirectory_NonExistingDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistent = Path.Combine(_testDir, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            FileSystemTools.ListDirectory(nonExistent).ToList());
    }

    [Fact]
    public void ListDirectory_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var outsidePath = Path.Combine(_testDir, "..");

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
            FileSystemTools.ListDirectory(outsidePath).ToList());
    }

    [Fact]
    public void DeleteFile_ExistingFile_DeletesFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "todelete.txt");
        File.WriteAllText(filePath, "content");

        // Act
        FileSystemTools.DeleteFile(filePath);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void DeleteFile_NonExistingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "nonexistent.txt");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            FileSystemTools.DeleteFile(filePath));
    }

    [Fact]
    public void DeleteFile_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var outsidePath = "/etc/passwd";

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
            FileSystemTools.DeleteFile(outsidePath));
    }

    [Fact]
    public void CreateDirectory_NewDirectory_CreatesIt()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "newdir");

        // Act
        FileSystemTools.CreateDirectory(dirPath);

        // Assert
        Assert.True(Directory.Exists(dirPath));
    }

    [Fact]
    public void CreateDirectory_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var outsidePath = Path.Combine(_testDir, "..", "malicious_dir");

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
            FileSystemTools.CreateDirectory(outsidePath));
    }

    [Fact]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "exists.txt");
        File.WriteAllText(filePath, "");

        // Act & Assert
        Assert.True(FileSystemTools.FileExists(filePath));
    }

    [Fact]
    public void FileExists_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "nonexistent.txt");

        // Act & Assert
        Assert.False(FileSystemTools.FileExists(filePath));
    }

    [Fact]
    public void FileExists_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var outsidePath = "/etc/passwd";

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
            FileSystemTools.FileExists(outsidePath));
    }

    [Fact]
    public void DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(FileSystemTools.DirectoryExists(_testDir));
    }

    [Fact]
    public void DirectoryExists_NonExistingDirectory_ReturnsFalse()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "nonexistent");

        // Act & Assert
        Assert.False(FileSystemTools.DirectoryExists(dirPath));
    }

    [Fact]
    public void DirectoryExists_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var outsidePath = Path.Combine(_testDir, "..");

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
            FileSystemTools.DirectoryExists(outsidePath));
    }
}
