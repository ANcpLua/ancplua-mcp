using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Ancplua.Mcp.Servers.Stdio.GitHubApps.Tools;

/// <summary>
/// Tools for interacting with Gemini Code Assist
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812", Justification = "Instantiated by MCP SDK via reflection.")]
[SuppressMessage("Design", "CA1515", Justification = "Tools are exposed publicly for MCP discovery.")]
internal sealed class GeminiCodeAssistTools
{
    /// <summary>
    /// Invoke Gemini Code Assist review on a pull request
    /// </summary>
    [McpServerTool]
    [Description("Invoke Gemini Code Assist to review a pull request")]
    public static Task<string> InvokeGeminiReview(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo,
        [Description("Pull request number")] int prNumber)
    {
        return Task.FromResult(
            $"To invoke Gemini Code Assist on PR #{prNumber}:\n\n" +
            $"1. Tag Gemini in a comment: @gemini-code-assist\n" +
            $"2. Use slash commands: /gemini review\n" +
            $"3. Or wait for automatic review (usually within 5 minutes)\n\n" +
            $"Gemini will provide:\n" +
            $"- Code quality feedback\n" +
            $"- Security analysis\n" +
            $"- Performance suggestions\n" +
            $"- Ready-to-commit fixes\n\n" +
            $"React with 👍 or 👎 to rate Gemini's suggestions.");
    }

    /// <summary>
    /// Configure Gemini Code Assist settings for a repository
    /// </summary>
    [McpServerTool]
    [Description("Get instructions for configuring Gemini Code Assist")]
    public static Task<string> ConfigureGemini(
        [Description("Repository owner")] string owner,
        [Description("Repository name")] string repo)
    {
        return Task.FromResult(
            $"To configure Gemini Code Assist for {owner}/{repo}:\n\n" +
            $"1. Create directory: .gemini/\n" +
            $"2. Add file: .gemini/config.yaml\n\n" +
            $"Example configuration:\n\n" +
            $"```yaml\n" +
            $"style_guides:\n" +
            $"  - PEP-8\n" +
            $"  - C# Coding Conventions\n" +
            $"  - .NET Design Guidelines\n\n" +
            $"exclude_paths:\n" +
            $"  - '**/bin/**'\n" +
            $"  - '**/obj/**'\n" +
            $"  - '**/node_modules/**'\n" +
            $"  - '**/*.generated.cs'\n\n" +
            $"review_focus:\n" +
            $"  - security\n" +
            $"  - performance\n" +
            $"  - maintainability\n" +
            $"  - best_practices\n\n" +
            $"languages:\n" +
            $"  - csharp\n" +
            $"  - yaml\n" +
            $"  - json\n" +
            $"```\n\n" +
            $"3. Commit and push the configuration\n" +
            $"4. Gemini will use these settings on future reviews");
    }
}
