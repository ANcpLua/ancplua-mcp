namespace Ancplua.Mcp.DebugTools.Models;

/// <summary>
/// Authenticated user claims information.
/// </summary>
public sealed record UserClaimsInfo
{
    /// <summary>
    /// Whether HTTP context is available.
    /// </summary>
    public required bool Available { get; init; }

    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public required bool IsAuthenticated { get; init; }

    /// <summary>
    /// Authentication type (e.g., "Bearer", "Cookie").
    /// </summary>
    public string? AuthenticationType { get; init; }

    /// <summary>
    /// Message when claims are not available.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// User claims as key-value pairs.
    /// </summary>
    public Dictionary<string, string>? Claims { get; init; }
}