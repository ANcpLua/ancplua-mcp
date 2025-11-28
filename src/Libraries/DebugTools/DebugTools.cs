using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Ancplua.Mcp.Libraries.DebugTools.Models;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.Libraries.DebugTools;

/// <summary>
/// MCP tools for debugging and introspection of server state, environment, and HTTP context.
/// </summary>
/// <remarks>
/// HTTP context tools gracefully degrade on stdio transport by returning Available = false.
/// </remarks>
[McpServerToolType]
public sealed class DebugToolset(IHttpContextAccessor? httpContextAccessor = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] SensitivePatterns =
    [
        "TOKEN", "KEY", "SECRET", "PASSWORD", "CREDENTIAL",
        "API_KEY", "APIKEY", "PRIVATE", "AUTH",
        "CONNECTION_STRING", "CONNECTIONSTRING"
    ];

    private static readonly DateTime ProcessStartTime = Process.GetCurrentProcess().StartTime;

    /// <summary>
    /// Returns all environment variables with sensitive values masked.
    /// </summary>
    [McpServerTool(Name = "debug_print_env")]
    [Description("Returns all environment variables with sensitive values masked. Useful for debugging configuration issues.")]
    public static EnvironmentInfo PrintEnvironment()
    {
        var variables = new Dictionary<string, string>();
        var maskedCount = 0;

        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key.ToString() ?? string.Empty;
            var value = entry.Value?.ToString() ?? string.Empty;

            if (IsSensitiveVariable(key))
            {
                variables[key] = "***MASKED***";
                maskedCount++;
            }
            else
            {
                variables[key] = value;
            }
        }

        return new EnvironmentInfo
        {
            Variables = variables,
            TotalCount = variables.Count,
            MaskedCount = maskedCount
        };
    }

    /// <summary>
    /// Returns server metadata including version, transport type, and runtime info.
    /// </summary>
    [McpServerTool(Name = "debug_get_server_info")]
    [Description("Returns server metadata including version, transport type, and runtime info.")]
    public ServerInfo GetServerInfo()
    {
        var assembly = Assembly.GetEntryAssembly();
        var serverName = assembly?.GetName().Name ?? "Unknown";
        var version = assembly?.GetName().Version?.ToString() ?? "0.0.0";

        // Determine transport type based on HTTP context availability
        var transport = httpContextAccessor?.HttpContext != null ? "http" : "stdio";

        return new ServerInfo
        {
            ServerName = serverName,
            Version = version,
            Transport = transport,
            DotNetVersion = Environment.Version.ToString(),
            OperatingSystem = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}",
            ProcessorCount = Environment.ProcessorCount,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Uptime = DateTime.Now - ProcessStartTime,
            ProcessId = Environment.ProcessId
        };
    }

    /// <summary>
    /// Returns HTTP request headers. Only available on HTTP transport.
    /// </summary>
    [McpServerTool(Name = "debug_get_http_headers")]
    [Description("Returns HTTP request headers. Only available on HTTP transport; returns unavailable message on stdio.")]
    public HttpContextInfo GetHttpHeaders()
    {
        var context = httpContextAccessor?.HttpContext;
        if (context == null)
        {
            return new HttpContextInfo
            {
                Available = false,
                Message = "No HTTP context available. This tool requires HTTP transport."
            };
        }

        var headers = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            // Mask Authorization header value
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                headers[header.Key] = "***MASKED***";
            }
            else
            {
                headers[header.Key] = string.Join(", ", header.Value.ToArray());
            }
        }

        return new HttpContextInfo
        {
            Available = true,
            Headers = headers,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
        };
    }

    /// <summary>
    /// Returns authenticated user claims. Only available on HTTP transport with authentication.
    /// </summary>
    [McpServerTool(Name = "debug_get_user_claims")]
    [Description("Returns authenticated user claims. Only available on HTTP transport; returns unavailable message on stdio.")]
    public UserClaimsInfo GetUserClaims()
    {
        var context = httpContextAccessor?.HttpContext;
        if (context == null)
        {
            return new UserClaimsInfo
            {
                Available = false,
                IsAuthenticated = false,
                Message = "No HTTP context available. This tool requires HTTP transport."
            };
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return new UserClaimsInfo
            {
                Available = true,
                IsAuthenticated = false,
                Message = "User is not authenticated."
            };
        }

        var claims = new Dictionary<string, string>();
        foreach (var claim in user.Claims)
        {
            // Avoid duplicate keys by appending index if needed
            var key = claim.Type;
            if (claims.ContainsKey(key))
            {
                var index = 1;
                while (claims.ContainsKey($"{key}_{index}"))
                {
                    index++;
                }
                key = $"{key}_{index}";
            }
            claims[key] = claim.Value;
        }

        return new UserClaimsInfo
        {
            Available = true,
            IsAuthenticated = true,
            AuthenticationType = user.Identity.AuthenticationType,
            Claims = claims
        };
    }

    /// <summary>
    /// Serializes the result as JSON for tool output.
    /// </summary>
    [McpServerTool(Name = "debug_get_all")]
    [Description("Returns all debug information (environment, server info, HTTP context, claims) in a single call.")]
    public string GetAllDebugInfo()
    {
        var result = new
        {
            environment = PrintEnvironment(),
            server = GetServerInfo(),
            httpContext = GetHttpHeaders(),
            userClaims = GetUserClaims()
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private static bool IsSensitiveVariable(string name)
    {
        var upperName = name.ToUpperInvariant();
        return SensitivePatterns.Any(pattern => upperName.Contains(pattern, StringComparison.Ordinal));
    }
}
