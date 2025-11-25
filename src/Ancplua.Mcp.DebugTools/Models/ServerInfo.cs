namespace Ancplua.Mcp.DebugTools.Models;

/// <summary>
/// Server metadata and runtime information.
/// </summary>
public sealed record ServerInfo
{
    /// <summary>
    /// Name of the MCP server.
    /// </summary>
    public required string ServerName { get; init; }

    /// <summary>
    /// Server version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Transport type: "stdio", "http", or "sse".
    /// </summary>
    public required string Transport { get; init; }

    /// <summary>
    /// .NET runtime version.
    /// </summary>
    public required string DotNetVersion { get; init; }

    /// <summary>
    /// Operating system description.
    /// </summary>
    public required string OperatingSystem { get; init; }

    /// <summary>
    /// Number of processors available.
    /// </summary>
    public required int ProcessorCount { get; init; }

    /// <summary>
    /// Current working directory.
    /// </summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>
    /// Server uptime since process start.
    /// </summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>
    /// Process ID.
    /// </summary>
    public required int ProcessId { get; init; }
}