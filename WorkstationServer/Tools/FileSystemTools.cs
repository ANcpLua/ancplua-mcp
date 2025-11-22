namespace WorkstationServer.Tools;

/// <summary>
/// Provides MCP tools for filesystem operations including reading, writing, and listing files.
/// </summary>
public class FileSystemTools
{
    /// <summary>
    /// Reads the contents of a file at the specified path.
    /// </summary>
    /// <param name="path">The absolute or relative path to the file.</param>
    /// <returns>The contents of the file as a string.</returns>
    public static async Task<string> ReadFileAsync(string path)
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
    public static async Task WriteFileAsync(string path, string content)
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
    public static IEnumerable<string> ListDirectory(string path)
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
    public static void DeleteFile(string path)
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
    public static void CreateDirectory(string path)
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
    public static bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// Checks if a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    public static bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }
}
