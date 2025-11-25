using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Ancplua.Mcp.GitHubAppsServer.Tools;

/// <summary>
/// Tools for interacting with Codecov and Codecov AI
/// </summary>
[McpServerToolType]
public static class CodecovTools
{
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Get coverage report for a repository
    /// </summary>
    [McpServerTool]
    [Description("Get code coverage report from Codecov for a specific repository and branch")]
    public static async Task<string> GetCoverage(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Branch name (optional, defaults to main)")] string? branch = null)
    {
        var token = Environment.GetEnvironmentVariable("CODECOV_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            return "Error: CODECOV_TOKEN environment variable not set";
        }

        var branchParam = branch ?? "main";
        var url = $"https://codecov.io/api/v2/github/{owner}/{repo}/branch/{branchParam}";

        HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

        try
        {
            var response = await HttpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: {response.StatusCode} - {content}";
            }

            return content;
        }
        catch (Exception ex)
        {
            return $"Error fetching coverage: {ex.Message}";
        }
    }

    /// <summary>
    /// Trigger Codecov AI review on a pull request
    /// </summary>
    [McpServerTool]
    [Description("Trigger Codecov AI to review a pull request")]
    public static Task<string> TriggerCodecovAiReview(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Pull request number")] int prNumber)
    {
        return Task.FromResult(
            $"To trigger Codecov AI review on PR #{prNumber}:\n\n" +
            $"Comment on the PR: @codecov-ai-reviewer review\n\n" +
            $"Or for test generation: @codecov-ai-reviewer test\n\n" +
            $"The Codecov AI bot will respond within a few minutes with AI-generated insights.");
    }
}
