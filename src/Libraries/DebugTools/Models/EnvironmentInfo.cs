namespace Ancplua.Mcp.Libraries.DebugTools.Models;

/// <summary>
/// Environment variable information with sensitive value masking.
/// </summary>
public sealed record EnvironmentInfo
{
    /// <summary>
    /// Environment variables with sensitive values masked.
    /// </summary>
    public required Dictionary<string, string> Variables { get; init; }

    /// <summary>
    /// Total number of environment variables.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Number of variables with masked values.
    /// </summary>
    public required int MaskedCount { get; init; }
}