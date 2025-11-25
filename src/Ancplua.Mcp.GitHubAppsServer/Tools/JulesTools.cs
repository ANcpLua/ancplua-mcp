using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ancplua.Mcp.GitHubAppsServer.Tools;

/// <summary>
/// Tools for interacting with Jules (Google Labs AI) via API.
/// Jules is an autonomous coding agent that creates plans, executes them,
/// and delivers work via commits/PRs - not review comments.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812", Justification = "Instantiated by MCP SDK via reflection.")]
[SuppressMessage("Performance", "CA1812", Justification = "Activated via MCP tool discovery/DI.")]
internal sealed class JulesTools
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;

    /// <summary>
    /// Initializes JulesTools with configuration for API access.
    /// </summary>
    public JulesTools(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);

        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["JULES_API_KEY"];
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("Jules");
        client.BaseAddress = new Uri("https://jules.googleapis.com/v1alpha/");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_apiKey))
        {
            // CAUTION: This adds the key to every request from this client instance
            client.DefaultRequestHeaders.Add("X-Goog-Api-Key", _apiKey);
        }
        return client;
    }

    /// <summary>
    /// Creates a new asynchronous Jules session via the API.
    /// </summary>
    [McpServerTool]
    [Description("Creates a new Jules AI coding session to work on a task. Jules is an autonomous agent that creates plans, executes code changes, and delivers work via PRs.")]
    public async Task<string> CreateJulesSession(
        [Description("Repository owner (e.g., 'AncpLua')")] string owner,
        [Description("Repository name (e.g., 'my-repo')")] string repo,
        [Description("The prompt describing the task for Jules")] string prompt,
        [Description("The starting branch (default: 'main')")] string branch = "main",
        [Description("Pull Request number (if the task relates to an existing PR)")] int? prNumber = null,
        [Description("Whether to require human approval of the plan (default: true)")] bool requirePlanApproval = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Error: JULES_API_KEY is not configured in the server environment.\n\n" +
                   "To configure:\n" +
                   "1. Get an API key from https://jules.google\n" +
                   "2. Set JULES_API_KEY environment variable\n" +
                   "3. Restart the MCP server";
        }

        var githubRepoContext = new Dictionary<string, object>
        {
            ["startingBranch"] = branch
        };

        if (prNumber.HasValue)
        {
            githubRepoContext["pullRequestNumber"] = prNumber.Value;
        }

        var requestBody = new
        {
            prompt,
            sourceContext = new
            {
                source = $"sources/github/{owner}/{repo}",
                githubRepoContext
            },
            // AUTO_CREATE_PR: Jules creates a PR but does not merge
            automationMode = "AUTO_CREATE_PR",
            requirePlanApproval,
            title = $"MCP Task: {(prompt.Length > 70 ? prompt[..70] + "..." : prompt)}"
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var client = CreateClient();

        try
        {
            // CA2234: Use Uri
            var response = await client.PostAsync(new Uri("sessions", UriKind.Relative), content).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var jsonDoc = JsonDocument.Parse(responseBody);
                var sessionId = jsonDoc.RootElement.GetProperty("id").GetString();
                var sessionUrl = $"https://jules.google/session/{sessionId}";

                return $"Jules session created successfully.\n\n" +
                       $"Session ID: {sessionId}\n" +
                       $"Monitor & Approve: {sessionUrl}\n\n" +
                       $"Workflow:\n" +
                       $"1. Jules analyzes the codebase\n" +
                       $"2. Creates an execution plan\n" +
                       $"3. Awaits human approval (if requirePlanApproval=true)\n" +
                       $"4. Executes the plan\n" +
                       $"5. Creates a PR with the changes\n\n" +
                       "Note: Jules delivers work via commits/PRs, not review comments.";
            }
            else
            {
                return $"Error creating Jules session.\n" +
                       $"Status Code: {response.StatusCode}\n" +
                       $"Response: {responseBody}";
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException) // CA1031: Specific exceptions
        {
            return $"Exception during Jules API call: {ex.Message}";
        }
    }

    /// <summary>
    /// Get information about Jules capabilities and configuration.
    /// </summary>
    [McpServerTool]
    [Description("Get information about Jules AI capabilities and how it differs from other AI reviewers")]
    public Task<string> GetJulesInfo()
    {
        var isConfigured = !string.IsNullOrEmpty(_apiKey);

        return Task.FromResult(
            $"Jules AI Agent (Google Labs)\n\n" +
            $"Configuration Status: {(isConfigured ? "Configured" : "Not configured (JULES_API_KEY missing)")}\n" +
            $"API Endpoint: https://jules.googleapis.com/v1alpha\n\n" +
            $"Key Characteristics:\n" +
            $"- Autonomous Agent: Jules writes code, not just suggestions\n" +
            $"- Plan-Based: Creates execution plan before making changes\n" +
            $"- Human Approval: Can require plan approval before execution\n" +
            $"- PR Delivery: Delivers work via commits/PRs, NOT review comments\n\n" +
            $"Automation Modes:\n" +
            $"- AUTO_CREATE_PR: Creates PR automatically (recommended)\n" +
            $"- MANUAL: User must manually apply changes\n\n" +
            $"Safety Features:\n" +
            $"- requirePlanApproval: true (default) - Human must approve plan\n" +
            $"- No auto-merge: Jules creates PRs but never merges them\n" +
            $"- Branch isolation: Changes on feature branches only\n\n" +
            $"Comparison to Other AI Tools:\n" +
            $"- CodeRabbit/Gemini: Leave review comments with suggestions\n" +
            $"- Jules: Actually implements changes and creates PRs\n" +
            $"- Copilot Workspace: Similar autonomous approach\n\n" +
            "Note: Jules API (v1alpha) does not support webhooks.\n" +
            "Auto-merge based on Jules completion is not possible.");
    }
}
