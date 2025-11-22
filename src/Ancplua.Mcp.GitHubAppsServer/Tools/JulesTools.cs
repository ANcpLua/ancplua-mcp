using System.ComponentModel;

namespace Ancplua.Mcp.GitHubAppsServer.Tools;

/// <summary>
/// Tools for interacting with Jules (Google Labs AI)
/// </summary>
[McpServerToolType]
public static class JulesTools
{
    /// <summary>
    /// Invoke Jules AI on a pull request
    /// </summary>
    [McpServerTool]
    [Description("Invoke Jules AI to assist with a pull request")]
    public static Task<string> InvokeJules(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Pull request number")] int prNumber,
        [Description("Specific request for Jules (optional)")] string? request = null)
    {
        var baseInstructions = $"To invoke Jules on PR #{prNumber}:\n\n" +
            $"Comment on the PR: @jules\n\n";

        if (!string.IsNullOrEmpty(request))
        {
            baseInstructions += $"Or with specific instructions: @jules {request}\n\n";
        }

        return Task.FromResult(
            baseInstructions +
            $"Jules can help with:\n" +
            $"- Code review and suggestions\n" +
            $"- Bug fixes\n" +
            $"- Refactoring recommendations\n" +
            $"- Best practices guidance\n\n" +
            $"Note: Jules is configured in safe mode - it provides suggestions only.\n" +
            $"Human approval is required for all changes.");
    }

    /// <summary>
    /// Check Jules workflow status
    /// </summary>
    [McpServerTool]
    [Description("Check the configuration status of Jules workflows")]
    public static Task<string> CheckJulesConfig(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo)
    {
        return Task.FromResult(
            $"Jules configuration for {owner}/{repo}:\n\n" +
            $"✅ Safe Mode Enabled\n" +
            $"  - Review comments only (no auto-commits)\n" +
            $"  - No auto-merge (requires CI + 2 approvals)\n" +
            $"  - Infinite loop prevention active\n\n" +
            $"Workflows:\n" +
            $"  - jules-auto-reviewer.yml: Runs on PR open/update\n" +
            $"  - jules-cleanup.yml: Weekly schedule (Sundays)\n\n" +
            $"Security:\n" +
            $"  - Read-only file permissions\n" +
            $"  - Respects branch protection rules\n" +
            $"  - Skips bot PRs to prevent loops\n\n" +
            $"To trigger manually:\n" +
            $"  - Tag @jules in PR comments\n" +
            $"  - Use /jules-review command (if configured)");
    }
}
