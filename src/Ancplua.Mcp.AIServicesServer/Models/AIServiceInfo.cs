using System.Collections.Generic;

namespace Ancplua.Mcp.AIServicesServer.Models;

/// <summary>
/// Information about an AI service.
/// </summary>
internal sealed record AiServiceInfo
{
    /// <summary>
    /// Service name (claude, jules, gemini, etc.)
    /// </summary>
    public required string Name
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value.Trim();
        }
    }

    /// <summary>
    /// Service type (conversational, task-automation, code-review, etc.)
    /// </summary>
    public required string Type
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value.Trim();
        }
    }

    /// <summary>
    /// Service status (active, inactive, error)
    /// </summary>
    public required string Status
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value.Trim();
        }
    }

    /// <summary>
    /// Service capabilities
    /// </summary>
    public required IReadOnlyList<string> Capabilities { get; init; } = [];

    /// <summary>
    /// API endpoint (if applicable)
    /// </summary>
    public string? ApiEndpoint { get; init; }

    /// <summary>
    /// Service description
    /// </summary>
    public string? Description { get; init; }
}
