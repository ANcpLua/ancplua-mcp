using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Ancplua.Mcp.GitHubAppsServer.Tools;

/// <summary>
/// Tools for interacting with CodeRabbit AI
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812", Justification = "Instantiated by MCP SDK via reflection.")]
[SuppressMessage("Design", "CA1515", Justification = "Tools are exposed publicly for MCP discovery.")]
internal sealed class CodeRabbitTools
{
    /// <summary>
    /// Trigger CodeRabbit AI review
    /// </summary>
    [McpServerTool]
    [Description("Trigger CodeRabbit AI to review a pull request")]
    public static Task<string> TriggerCodeRabbitReview(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Pull request number")] int prNumber)
    {
        return Task.FromResult(
            $"To trigger CodeRabbit review on PR #{prNumber}:\n\n" +
            $"Comment on the PR: @coderabbitai review\n\n" +
            $"CodeRabbit will provide:\n" +
            $"- PR summary\n" +
            $"- Line-by-line code analysis\n" +
            $"- Actionable suggestions (ready to commit)\n" +
            $"- Security and performance insights\n\n" +
            $"Additional commands:\n" +
            $"- @coderabbitai help - Show available commands\n" +
            $"- @coderabbitai pause - Pause reviews for this PR\n" +
            $"- @coderabbitai resume - Resume reviews");
    }

    /// <summary>
    /// Chat with CodeRabbit about code
    /// </summary>
    [McpServerTool]
    [Description("Ask CodeRabbit AI a question about the code in a PR")]
    public static Task<string> AskCodeRabbit(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Pull request number")] int prNumber,
        [Description("Question to ask CodeRabbit")] string question)
    {
        return Task.FromResult(
            $"To ask CodeRabbit your question on PR #{prNumber}:\n\n" +
            $"Comment on the PR:\n\n" +
            $"@coderabbitai {question}\n\n" +
            $"Examples:\n" +
            $"- @coderabbitai explain this function\n" +
            $"- @coderabbitai suggest optimizations for this code\n" +
            $"- @coderabbitai is this code secure?\n\n" +
            $"CodeRabbit will respond with detailed, context-aware answers.");
    }
}
