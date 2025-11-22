using System.ComponentModel;
using System.Text.Json;
using Ancplua.Mcp.AIServicesServer.Models;
using ModelContextProtocol.Server;

namespace Ancplua.Mcp.AIServicesServer.Tools;

/// <summary>
/// MCP tools for discovering and querying AI services.
/// </summary>
[McpServerToolType]
public static class ServiceDiscoveryTools
{
    /// <summary>
    /// Lists all configured AI services and their status.
    /// </summary>
    [McpServerTool]
    [Description("Lists all configured AI services and their current status")]
    public static Task<string> ListAIServices()
    {
        var services = new[]
        {
            new AIServiceInfo
            {
                Name = "claude",
                Type = "conversational",
                Status = "active",
                Capabilities = ["code-review", "generation", "refactoring", "analysis"],
                ApiEndpoint = "https://api.anthropic.com",
                Description = "Anthropic Claude AI assistant"
            },
            new AIServiceInfo
            {
                Name = "jules",
                Type = "task-automation",
                Status = "active",
                Capabilities = ["pr-review", "code-fixes", "cleanup", "refactoring"],
                ApiEndpoint = "https://jules.google.com/api",
                Description = "Google Jules AI coding agent"
            },
            new AIServiceInfo
            {
                Name = "gemini",
                Type = "conversational",
                Status = "active",
                Capabilities = ["code-review", "generation", "analysis"],
                ApiEndpoint = "https://generativelanguage.googleapis.com",
                Description = "Google Gemini AI model"
            },
            new AIServiceInfo
            {
                Name = "chatgpt",
                Type = "conversational",
                Status = "active",
                Capabilities = ["code-review", "generation", "explanation"],
                ApiEndpoint = "https://api.openai.com",
                Description = "OpenAI ChatGPT"
            },
            new AIServiceInfo
            {
                Name = "copilot",
                Type = "code-completion",
                Status = "active",
                Capabilities = ["code-completion", "generation", "chat"],
                Description = "GitHub Copilot"
            },
            new AIServiceInfo
            {
                Name = "coderabbit",
                Type = "code-review",
                Status = "active",
                Capabilities = ["pr-review", "code-quality", "security"],
                Description = "CodeRabbit AI code reviewer"
            },
            new AIServiceInfo
            {
                Name = "codecov",
                Type = "test-analysis",
                Status = "active",
                Capabilities = ["coverage-analysis", "test-quality"],
                ApiEndpoint = "https://api.codecov.io",
                Description = "Codecov test coverage analysis"
            }
        };

        return Task.FromResult(JsonSerializer.Serialize(services, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    /// <summary>
    /// Gets detailed capabilities of a specific AI service.
    /// </summary>
    [McpServerTool]
    [Description("Gets detailed capabilities and configuration of a specific AI service")]
    public static Task<string> GetServiceCapabilities(
        [Description("Service name (claude, jules, gemini, chatgpt, copilot, coderabbit, codecov)")]
        string serviceName)
    {
        object capabilities = serviceName.ToLowerInvariant() switch
        {
            "claude" => new
            {
                service = "claude",
                model = "claude-sonnet-4.5",
                capabilities = new[]
                {
                    new { name = "code-review", description = "Comprehensive code review with architecture analysis" },
                    new { name = "generation", description = "Generate code from natural language descriptions" },
                    new { name = "refactoring", description = "Suggest and implement code refactorings" },
                    new { name = "analysis", description = "Deep code analysis and documentation" }
                },
                rateLimit = "50 requests/minute",
                maxTokens = 200000
            },
            "jules" => new
            {
                service = "jules",
                capabilities = new[]
                {
                    new { name = "pr-review", description = "Automated pull request review" },
                    new { name = "code-fixes", description = "Apply automated code fixes" },
                    new { name = "cleanup", description = "Remove dead code and optimize" },
                    new { name = "refactoring", description = "Automated refactoring" }
                },
                rateLimit = "Varies by subscription",
                features = new[] { "multi-file-editing", "git-integration" }
            },
            "gemini" => new
            {
                service = "gemini",
                model = "gemini-3.0-pro",
                capabilities = new[]
                {
                    new { name = "code-review", description = "AI-powered code review" },
                    new { name = "generation", description = "Code generation" },
                    new { name = "analysis", description = "Code analysis and suggestions" }
                },
                rateLimit = "60 requests/minute (free tier)",
                maxTokens = 1000000
            },
            "chatgpt" => new
            {
                service = "chatgpt",
                model = "gpt-4-turbo",
                capabilities = new[]
                {
                    new { name = "code-review", description = "Code review and suggestions" },
                    new { name = "generation", description = "Code generation" },
                    new { name = "explanation", description = "Explain complex code" }
                },
                rateLimit = "Varies by tier",
                maxTokens = 128000
            },
            "copilot" => new
            {
                service = "copilot",
                capabilities = new[]
                {
                    new { name = "code-completion", description = "Real-time code completion" },
                    new { name = "generation", description = "Generate code from comments" },
                    new { name = "chat", description = "Interactive coding assistant" }
                },
                features = new[] { "ide-integration", "context-aware-suggestions" }
            },
            "coderabbit" => new
            {
                service = "coderabbit",
                capabilities = new[]
                {
                    new { name = "pr-review", description = "Automated PR review" },
                    new { name = "code-quality", description = "Code quality analysis" },
                    new { name = "security", description = "Security vulnerability detection" }
                },
                features = new[] { "incremental-review", "ai-summaries", "smart-chat" }
            },
            "codecov" => new
            {
                service = "codecov",
                capabilities = new[]
                {
                    new { name = "coverage-analysis", description = "Test coverage tracking" },
                    new { name = "test-quality", description = "Test quality metrics" }
                },
                features = new[] { "pr-comments", "coverage-reports", "trend-analysis" }
            },
            _ => new
            {
                error = $"Unknown service: {serviceName}",
                availableServices = new[] { "claude", "jules", "gemini", "chatgpt", "copilot", "coderabbit", "codecov" }
            }
        };

        return Task.FromResult(JsonSerializer.Serialize(capabilities, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}
