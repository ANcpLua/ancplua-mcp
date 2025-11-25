using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Ancplua.Mcp.GitHubAppsServer.Tools;

/// <summary>
/// Tools for orchestrating multiple AI code review services
/// </summary>
[McpServerToolType]
public static class AiOrchestrationTools
{
    /// <summary>
    /// Trigger all AI reviewers on a pull request
    /// </summary>
    [McpServerTool]
    [Description("Invoke all configured AI code reviewers (Gemini, CodeRabbit, Jules, Copilot, ChatGPT) on a PR")]
    public static Task<string> TriggerAllReviewers(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Pull request number")] int prNumber)
    {
        return Task.FromResult(
            $"To invoke ALL AI reviewers on PR #{prNumber}, add these comments:\n\n" +
            $"1. **Gemini Code Assist**: @gemini-code-assist\n" +
            $"2. **CodeRabbit AI**: @coderabbitai review\n" +
            $"3. **Codecov AI**: @codecov-ai-reviewer review\n" +
            $"4. **GitHub Copilot**: (automatic, configured in ruleset)\n\n" +
            $"Alternatively, create a single comment with all tags:\n\n" +
            $"```\n" +
            $"@gemini-code-assist @coderabbitai @codecov-ai-reviewer please review\n" +
            $"```\n\n" +
            $"Note: Jules is NOT triggered via @mentions - use /jules command or API.\n" +
            $"Jules is an autonomous agent that creates PRs, not review comments.\n\n" +
            $"All other reviewers are configured to run automatically on PR creation.\n" +
            $"This command is useful for re-triggering reviews after updates.");
    }

    /// <summary>
    /// Get AI review summary across all services
    /// </summary>
    [McpServerTool]
    [Description("Get a summary of all AI review comments on a PR")]
    public static Task<string> GetAiReviewSummary(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Pull request number")] int prNumber)
    {
        return Task.FromResult(
            $"To view AI review summary for PR #{prNumber}:\n\n" +
            $"Check these sections in the PR:\n\n" +
            $"1. **Gemini Code Assist**\n" +
            $"   - Look for comments from @gemini-code-assist\n" +
            $"   - Usually reviews within 5 minutes\n" +
            $"   - Provides ready-to-commit suggestions\n\n" +
            $"2. **CodeRabbit AI**\n" +
            $"   - PR Summary at the top\n" +
            $"   - Line-by-line review comments\n" +
            $"   - Chat interface for questions\n\n" +
            $"3. **Codecov AI**\n" +
            $"   - Coverage report\n" +
            $"   - Test generation suggestions\n" +
            $"   - Coverage improvement recommendations\n\n" +
            $"4. **Jules (Google Labs)**\n" +
            $"   - Autonomous Agent: Does NOT leave review comments\n" +
            $"   - Delivers work by pushing commits or creating new PRs\n" +
            $"   - Check associated Jules session link for plan and status\n\n" +
            $"5. **GitHub Copilot**\n" +
            $"   - Automatic review in PR checks\n" +
            $"   - Security and quality analysis\n" +
            $"   - Integration with static analysis tools\n\n" +
            $"To consolidate feedback:\n" +
            $"- Review all bot comments\n" +
            $"- Identify common themes\n" +
            $"- Prioritize security and critical issues\n" +
            $"- Address high-confidence suggestions first");
    }

    /// <summary>
    /// Compare AI reviewer capabilities
    /// </summary>
    [McpServerTool]
    [Description("Get a comparison of AI reviewer capabilities and when to use each")]
    public static Task<string> CompareAiReviewers()
    {
        return Task.FromResult(
            "AI Code Reviewer Comparison:\n\n" +
            "**Gemini Code Assist (FREE)**\n" +
            "✅ Best for: Quick, comprehensive reviews\n" +
            "✅ Strengths: Fast (5 min), ready-to-commit fixes, style guide enforcement\n" +
            "✅ Use when: You want immediate feedback on general code quality\n\n" +
            "**CodeRabbit AI ($15-30/month)**\n" +
            "✅ Best for: In-depth analysis, team collaboration\n" +
            "✅ Strengths: Chat interface, Jira integration, detailed reports\n" +
            "✅ Use when: You need advanced features or project management integration\n\n" +
            "**Codecov AI (FREE beta)**\n" +
            "✅ Best for: Test coverage and test generation\n" +
            "✅ Strengths: Coverage insights, AI test generation, quality metrics\n" +
            "✅ Use when: You want to improve test coverage\n\n" +
            "**Jules (Google Labs, FREE)**\n" +
            "✅ Best for: Autonomous code changes, not reviews\n" +
            "✅ Strengths: Creates PRs with actual code changes, plan-based execution\n" +
            "✅ Use when: You want an agent to implement changes, not just suggest them\n" +
            "⚠️ Note: Jules creates PRs, it does NOT leave review comments\n\n" +
            "**GitHub Copilot (Included)**\n" +
            "✅ Best for: Native GitHub integration\n" +
            "✅ Strengths: Automated ruleset enforcement, security scanning\n" +
            "✅ Use when: You want official GitHub AI integration\n\n" +
            "**Claude for GitHub (Anthropic, FREE)**\n" +
            "✅ Best for: Complex reasoning, architectural review\n" +
            "✅ Strengths: Deep analysis, context understanding, code changes\n" +
            "✅ Use when: You need human-like reasoning on complex changes\n\n" +
            "**ChatGPT Codex (OpenAI)**\n" +
            "✅ Best for: Research, deep dives, codebase understanding\n" +
            "✅ Strengths: GitHub connector, deep research capability\n" +
            "✅ Use when: You want to understand complex code patterns\n\n" +
            "**Recommendation:**\n" +
            "Use all reviewers for maximum coverage, but prioritize:\n" +
            "1. Gemini (general quality)\n" +
            "2. Copilot (security)\n" +
            "3. Codecov (coverage)\n" +
            "4. CodeRabbit or Claude (deep analysis)");
    }
}
