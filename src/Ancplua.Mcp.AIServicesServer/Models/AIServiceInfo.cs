namespace Ancplua.Mcp.AIServicesServer.Models;

/// <summary>
/// Information about an AI service.
/// </summary>
public record AiServiceInfo
{
    /// <summary>
    /// Service name (claude, jules, gemini, etc.)
    /// </summary>
    public required string Name { get; init => field = value.Trim(); }

    /// <summary>
    /// Service type (conversational, task-automation, code-review, etc.)
    /// </summary>
    public required string Type { get; init => field = value.Trim(); }

    /// <summary>
    /// Service status (active, inactive, error)
    /// </summary>
    public required string Status { get; init => field = value.Trim(); }

    /// <summary>
    /// Service capabilities
    /// </summary>
    public required string[] Capabilities { get; init; }

    /// <summary>
    /// API endpoint (if applicable)
    /// </summary>
    public string? ApiEndpoint { get; init; }

    /// <summary>
    /// Service description
    /// </summary>
    public string? Description { get; init; }
}
