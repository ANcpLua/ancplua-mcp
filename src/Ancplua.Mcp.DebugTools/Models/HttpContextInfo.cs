namespace Ancplua.Mcp.DebugTools.Models;

/// <summary>
/// HTTP request context information.
/// </summary>
public sealed record HttpContextInfo
{
    /// <summary>
    /// Whether HTTP context is available.
    /// </summary>
    public required bool Available { get; init; }

    /// <summary>
    /// Message when context is not available.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// HTTP request headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// HTTP method (GET, POST, etc.).
    /// </summary>
    public string? Method { get; init; }

    /// <summary>
    /// Request path.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Query string.
    /// </summary>
    public string? QueryString { get; init; }

    /// <summary>
    /// Remote IP address.
    /// </summary>
    public string? RemoteIpAddress { get; init; }
}