using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.WorkstationServer.Tools;

/// <summary>
/// Provides MCP tools for filesystem operations including reading, writing, and listing files.
/// </summary>
[McpServerToolType]
public static class FileSystemTools
{
    /// <summary>
    /// Reads the contents of a file at the specified path.
    /// </summary>
    /// <param name="path">The absolute or relative path to the file.</param>
    /// <returns>The contents of the file as a string.</returns>
    [McpServerTool]
    [Description("Reads the contents of a file at the specified path")]
    public static async Task<string> ReadFileAsync(
        [Description("The absolute or relative path to the file")] string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}");
        }

        return await File.ReadAllTextAsync(path);
    }

    /// <summary>
    /// Writes content to a file at the specified path.
    /// </summary>
    /// <param name="path">The absolute or relative path to the file.</param>
    /// <param name="content">The content to write to the file.</param>
    [McpServerTool]
    [Description("Writes content to a file at the specified path")]
    public static async Task WriteFileAsync(
        [Description("The absolute or relative path to the file")] string path,
        [Description("The content to write to the file")] string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content);
    }

    /// <summary>
    /// Lists files and directories at the specified path.
    /// </summary>
    /// <param name="path">The directory path to list.</param>
    /// <returns>A collection of file and directory names.</returns>
    [McpServerTool]
    [Description("Lists files and directories at the specified path")]
    public static IEnumerable<string> ListDirectory(
        [Description("The directory path to list")] string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        return Directory.EnumerateFileSystemEntries(path);
    }

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="path">The path to the file to delete.</param>
    [McpServerTool]
    [Description("Deletes a file at the specified path")]
    public static void DeleteFile(
        [Description("The path to the file to delete")] string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The path to the directory to create.</param>
    [McpServerTool]
    [Description("Creates a directory at the specified path")]
    public static void CreateDirectory(
        [Description("The path to the directory to create")] string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    [McpServerTool]
    [Description("Checks if a file exists at the specified path")]
    public static bool FileExists(
        [Description("The path to check")] string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// Checks if a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    [McpServerTool]
    [Description("Checks if a directory exists at the specified path")]
    public static bool DirectoryExists(
        [Description("The path to check")] string path)
    {
        return Directory.Exists(path);
    }
}
