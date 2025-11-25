using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.CoreTools.Tools;

/// <summary>
/// Provides MCP tools for filesystem operations including reading, writing, and listing files.
/// </summary>
/// <remarks>
/// <para>
/// <b>Security:</b> All paths are validated to be within the allowed base directory
/// (defaults to current working directory) to prevent path traversal attacks.
/// </para>
/// <para>
/// Configure the allowed base directory via the <see cref="AllowedBasePath"/> property
/// or the FILESYSTEM_TOOLS_BASE_PATH environment variable.
/// </para>
/// </remarks>
[McpServerToolType]
[SuppressMessage("Design", "CA1052", Justification = "MCP tools are discovered via generic registration and only expose static members.")]
public class FileSystemTools
{
    private static readonly object _lock = new();

    /// <summary>
    /// Gets or sets the allowed base path for filesystem operations.
    /// All file operations are restricted to this directory and its subdirectories.
    /// </summary>
    /// <remarks>
    /// Defaults to the FILESYSTEM_TOOLS_BASE_PATH environment variable if set,
    /// otherwise uses the current working directory.
    /// </remarks>
    public static string AllowedBasePath
    {
        get
        {
            lock (_lock)
            {
                return field ??= GetDefaultBasePath();
            }
        }
        set
        {
            lock (_lock)
            {
                field = Path.GetFullPath(value);
            }
        }
    }

    private static string GetDefaultBasePath()
    {
        var envPath = Environment.GetEnvironmentVariable("FILESYSTEM_TOOLS_BASE_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            return Path.GetFullPath(envPath);
        }
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Validates that a path is within the allowed base directory.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>The normalized full path if valid.</returns>
    /// <exception cref="UnauthorizedAccessException">If path is outside allowed directory.</exception>
    private static string ValidateAndNormalizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Resolve to absolute path
        var fullPath = Path.GetFullPath(path);
        var basePath = AllowedBasePath;

        // Ensure the path is within the allowed base directory
        // Use case-insensitive comparison on Windows, case-sensitive on Unix
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // Normalize paths with trailing separator for proper prefix checking
        var normalizedBase = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Check if path equals base or starts with base + separator
        var isWithinBase = normalizedPath.Equals(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), comparison)
            || (normalizedPath + Path.DirectorySeparatorChar).StartsWith(normalizedBase, comparison);

        if (!isWithinBase)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Path '{path}' is outside the allowed directory '{basePath}'.");
        }

        return fullPath;
    }

    /// <summary>
    /// Reads the contents of a file at the specified path.
    /// </summary>
    /// <remarks>
    /// Path must be within the allowed base directory.
    /// </remarks>
    [McpServerTool]
    [Description("Reads the contents of a file at the specified path (must be within allowed directory)")]
    public static async Task<string> ReadFileAsync(
        [Description("The absolute or relative path to the file")] string path,
        CancellationToken cancellationToken = default)
    {
        var validPath = ValidateAndNormalizePath(path);

        if (!File.Exists(validPath))
        {
            throw new FileNotFoundException($"File not found: {path}", path);
        }

        return await File.ReadAllTextAsync(validPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes content to a file at the specified path.
    /// </summary>
    /// <remarks>
    /// Path must be within the allowed base directory. Parent directories are created if needed.
    /// </remarks>
    [McpServerTool]
    [Description("Writes content to a file at the specified path (must be within allowed directory)")]
    public static async Task WriteFileAsync(
        [Description("The absolute or relative path to the file")] string path,
        [Description("The content to write to the file")] string content,
        CancellationToken cancellationToken = default)
    {
        var validPath = ValidateAndNormalizePath(path);

        var directory = Path.GetDirectoryName(validPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(validPath, content, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists files and directories at the specified path.
    /// </summary>
    /// <remarks>
    /// Path must be within the allowed base directory. Returns relative names, not full paths.
    /// </remarks>
    [McpServerTool]
    [Description("Lists files and directories at the specified path (must be within allowed directory)")]
    public static IEnumerable<string> ListDirectory(
        [Description("The directory path to list")] string path)
    {
        var validPath = ValidateAndNormalizePath(path);

        if (!Directory.Exists(validPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        // Return just the names, not full paths, for security
        return Directory.EnumerateFileSystemEntries(validPath)
            .Select(Path.GetFileName)
            .Where(name => name != null)!;
    }

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <remarks>
    /// Path must be within the allowed base directory.
    /// </remarks>
    [McpServerTool]
    [Description("Deletes a file at the specified path (must be within allowed directory)")]
    public static void DeleteFile(
        [Description("The path to the file to delete")] string path)
    {
        var validPath = ValidateAndNormalizePath(path);

        if (!File.Exists(validPath))
        {
            throw new FileNotFoundException($"File not found: {path}", path);
        }

        File.Delete(validPath);
    }

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <remarks>
    /// Path must be within the allowed base directory.
    /// </remarks>
    [McpServerTool]
    [Description("Creates a directory at the specified path (must be within allowed directory)")]
    public static void CreateDirectory(
        [Description("The path to the directory to create")] string path)
    {
        var validPath = ValidateAndNormalizePath(path);

        if (!Directory.Exists(validPath))
        {
            Directory.CreateDirectory(validPath);
        }
    }

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <remarks>
    /// Path must be within the allowed base directory.
    /// </remarks>
    [McpServerTool]
    [Description("Checks if a file exists at the specified path (must be within allowed directory)")]
    public static bool FileExists(
        [Description("The path to check")] string path)
    {
        var validPath = ValidateAndNormalizePath(path);
        return File.Exists(validPath);
    }

    /// <summary>
    /// Checks if a directory exists at the specified path.
    /// </summary>
    /// <remarks>
    /// Path must be within the allowed base directory.
    /// </remarks>
    [McpServerTool]
    [Description("Checks if a directory exists at the specified path (must be within allowed directory)")]
    public static bool DirectoryExists(
        [Description("The path to check")] string path)
    {
        var validPath = ValidateAndNormalizePath(path);
        return Directory.Exists(validPath);
    }
}
