# Spec-004: API Integration Implementation (v2.0)

## Overview

This specification defines the transition from instruction-based tools (v1.0) to API-integrated tools (v2.0) for the **Ancplua.Mcp.GitHubAppsServer**. This server will house the direct API clients for GitHub, Codecov, and other services, enabling true automation.

## Problem Statement

**Current State (v1.0)**:
- Tools in `GitHubAppsServer` return instruction strings.
- No actual API calls to GitHub or Codecov.
- Users must manually execute suggested commands.

**Target State (v2.0)**:
- **GitHubAppsServer** implements actual API calls (Octokit, HTTP).
- **AIServicesServer** (Orchestrator) consumes these tools or directs the user to them.
- Full automation layer with token authentication.

## Goals

1. **True Automation**: Tools perform actual operations.
2. **Separation of Concerns**: API clients reside in `GitHubAppsServer`.
3. **Authentication**: Secure token management.

## Architecture

### Server Responsibility

*   **Ancplua.Mcp.GitHubAppsServer**: The "Worker". Contains `GitHubClient`, `CodecovClient`, etc. Exposes granular tools like `TriggerGemini`, `GetCodecovReport`.
*   **Ancplua.Mcp.AIServicesServer**: The "Orchestrator". Composes high-level workflows (e.g., "Review PR") by calling tools on the GitHubAppsServer (logically, via the MCP host).

### Tool Evolution: v1.0 → v2.0

#### Example: TriggerAllReviewers (on GitHubAppsServer)

**v1.0 (Instruction-Based)**:
```csharp
// Returns string instructions
public static Task<string> InvokeJules(...) { ... }
```

**v2.0 (API-Integrated)**:
```csharp
[McpServerTool]
public static async Task<ReviewTriggerResult> InvokeJules(...)
{
    var github = GitHubClientFactory.Create();
    // Actual API call
    await github.Issue.Comment.Create(owner, repo, prNumber, "/jules-review");
    // ...
}
```

## API Integrations

### 1. GitHub API (Octokit.NET)

**Package**: `Octokit` v9.0+

**Authentication**:
```csharp
public static class GitHubClientFactory
{
    private static IGitHubClient? _client;

    public static IGitHubClient Create()
    {
        if (_client != null)
            return _client;

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable not set");

        _client = new GitHubClient(new ProductHeaderValue("ancplua-mcp"))
        {
            Credentials = new Credentials(token)
        };

        return _client;
    }
}
```

**Required Scopes**:
- `repo` - Full repository access
- `read:org` - Read organization information
- `workflow` - Trigger workflows

**Rate Limiting**:
```csharp
public static class GitHubRateLimiter
{
    private static readonly SemaphoreSlim _rateLimiter = new(100, 100); // 100 concurrent requests

    public static async Task<T> ExecuteWithRateLimiting<T>(Func<Task<T>> apiCall)
    {
        await _rateLimiter.WaitAsync();
        try
        {
            return await apiCall();
        }
        finally
        {
            // Release after 600ms (100 req/min)
            _ = Task.Delay(600).ContinueWith(_ => _rateLimiter.Release());
        }
    }
}
```

**Error Handling**:
```csharp
public static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> apiCall, int maxRetries = 3)
{
    var policy = Policy
        .Handle<ApiException>()
        .WaitAndRetryAsync(
            retryCount: maxRetries,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (exception, timeSpan, retry, context) =>
            {
                Console.Error.WriteLine($"Retry {retry}/{maxRetries} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
            });

    return await policy.ExecuteAsync(apiCall);
}
```

### 2. Codecov API

**Base URL**: `https://codecov.io/api/v2`

**Authentication**:
```csharp
public static class CodecovClientFactory
{
    private static HttpClient? _client;

    public static HttpClient Create()
    {
        if (_client != null)
            return _client;

        var token = Environment.GetEnvironmentVariable("CODECOV_TOKEN");

        _client = new HttpClient
        {
            BaseAddress = new Uri("https://codecov.io/api/v2")
        };

        if (!string.IsNullOrEmpty(token))
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return _client;
    }
}
```

**Get Coverage**:
```csharp
[McpServerTool]
[Description("Get test coverage metrics for a repository")]
public static async Task<CoverageMetrics> GetCoverage(
    [Description("Repository owner")] string owner,
    [Description("Repository name")] string repo)
{
    var client = CodecovClientFactory.Create();
    var response = await client.GetAsync($"/github/{owner}/repos/{repo}/coverage");

    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<CodecovCoverageResponse>(json);

    return new CoverageMetrics
    {
        OverallCoverage = data.Coverage.Percentage,
        TrendDirection = data.Coverage.Trend,
        LastUpdated = data.Coverage.LastUpdated
    };
}
```

### 3. CodeRabbit Integration (Comment-Based)

**Strategy**: CodeRabbit has no direct API, so integration is via GitHub comments and monitoring.

```csharp
[McpServerTool]
[Description("Trigger CodeRabbit review and wait for response")]
public static async Task<CodeRabbitReview> TriggerCodeRabbitReview(
    [Description("Repository owner")] string owner,
    [Description("Repository name")] string repo,
    [Description("Pull request number")] int prNumber,
    [Description("Optional review instructions")] string? instructions = null)
{
    var github = GitHubClientFactory.Create();

    // Trigger review
    var comment = instructions != null
        ? $"@coderabbitai review {instructions}"
        : "@coderabbitai review";

    await github.Issue.Comment.Create(owner, repo, prNumber, comment);

    // Poll for response (max 60 seconds)
    for (int i = 0; i < 12; i++)
    {
        await Task.Delay(5000); // Wait 5 seconds

        var comments = await github.Issue.Comment.GetAllForIssue(owner, repo, prNumber);
        var coderabbitComment = comments
            .Where(c => c.User.Login == "coderabbitai")
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        if (coderabbitComment != null &&
            coderabbitComment.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1))
        {
            return new CodeRabbitReview
            {
                Status = "Completed",
                ReviewText = coderabbitComment.Body,
                ReviewUrl = coderabbitComment.HtmlUrl
            };
        }
    }

    return new CodeRabbitReview
    {
        Status = "Pending",
        ReviewText = "CodeRabbit review is in progress. Check the PR for updates.",
        ReviewUrl = $"https://github.com/{owner}/{repo}/pull/{prNumber}"
    };
}
```

### 4. Gemini Code Assist

**Options**:
1. **Comment-based** (MVP): Post `@gemini-code-assist` comment
2. **Vertex AI API** (Future): Direct API calls to Google Cloud Vertex AI

**v2.0 Implementation** (Comment-based):
```csharp
[McpServerTool]
[Description("Invoke Gemini Code Assist for code review")]
public static async Task<GeminiReview> InvokeGeminiReview(
    [Description("Repository owner")] string owner,
    [Description("Repository name")] string repo,
    [Description("Pull request number")] int prNumber)
{
    var github = GitHubClientFactory.Create();

    await github.Issue.Comment.Create(owner, repo, prNumber, "@gemini-code-assist");

    return new GeminiReview
    {
        Status = "Triggered",
        Message = "Gemini Code Assist has been invoked. Check the PR for review feedback.",
        PullRequestUrl = $"https://github.com/{owner}/{repo}/pull/{prNumber}"
    };
}
```

### 5. Jules Integration (GitHub Actions)

**Strategy**: Jules runs via GitHub Actions workflow, triggered by `/jules-review` comment.

```csharp
[McpServerTool]
[Description("Invoke Jules AI agent for PR review and fixes")]
public static async Task<JulesInvocation> InvokeJules(
    [Description("Repository owner")] string owner,
    [Description("Repository name")] string repo,
    [Description("Pull request number")] int prNumber)
{
    var github = GitHubClientFactory.Create();

    // Trigger Jules via comment
    await github.Issue.Comment.Create(owner, repo, prNumber, "/jules-review");

    // Check if workflow exists
    var workflows = await github.Actions.Workflows.List(owner, repo);
    var julesWorkflow = workflows.Workflows.FirstOrDefault(w => w.Name.Contains("Jules"));

    if (julesWorkflow == null)
    {
        return new JulesInvocation
        {
            Status = "NotConfigured",
            Message = "Jules workflow not found. Please configure .github/workflows/jules.yml"
        };
    }

    return new JulesInvocation
    {
        Status = "Triggered",
        Message = "Jules has been invoked. Monitor workflow runs for progress.",
        WorkflowUrl = julesWorkflow.HtmlUrl,
        PullRequestUrl = $"https://github.com/{owner}/{repo}/pull/{prNumber}"
    };
}
```

## Models

### Service Trigger Result
```csharp
public class ServiceTriggerResult
{
    public required string Service { get; set; }
    public required string Status { get; set; } // "Triggered" | "Failed"
    public DateTime? Timestamp { get; set; }
    public string? Error { get; set; }
}
```

### Review Trigger Result
```csharp
public class ReviewTriggerResult
{
    public required string PullRequestUrl { get; set; }
    public int TriggeredServices { get; set; }
    public int FailedServices { get; set; }
    public required ServiceTriggerResult[] Results { get; set; }
}
```

### Coverage Metrics
```csharp
public class CoverageMetrics
{
    public double OverallCoverage { get; set; }
    public string? TrendDirection { get; set; } // "up" | "down" | "stable"
    public DateTime? LastUpdated { get; set; }
}
```

### CodeRabbit Review
```csharp
public class CodeRabbitReview
{
    public required string Status { get; set; } // "Completed" | "Pending" | "Failed"
    public required string ReviewText { get; set; }
    public required string ReviewUrl { get; set; }
}
```

### Gemini Review
```csharp
public class GeminiReview
{
    public required string Status { get; set; } // "Triggered" | "Failed"
    public required string Message { get; set; }
    public required string PullRequestUrl { get; set; }
}
```

### Jules Invocation
```csharp
public class JulesInvocation
{
    public required string Status { get; set; } // "Triggered" | "NotConfigured" | "Failed"
    public required string Message { get; set; }
    public string? WorkflowUrl { get; set; }
    public required string PullRequestUrl { get; set; }
}
```

## Implementation Plan

### Phase 1: GitHub API Integration (Week 1)
- [ ] Add Octokit.NET package
- [ ] Implement GitHubClientFactory
- [ ] Implement GitHubRateLimiter
- [ ] Convert TriggerAllReviewers to API calls
- [ ] Convert GetAIReviewSummary to API calls
- [ ] Unit tests for GitHub integration

### Phase 2: Codecov + CodeRabbit (Week 2)
- [ ] Implement CodecovClientFactory
- [ ] GetCoverage implementation
- [ ] TriggerCodeRabbitReview with polling
- [ ] Integration tests

### Phase 3: Gemini + Jules (Week 3)
- [ ] InvokeGeminiReview implementation
- [ ] InvokeJules implementation
- [ ] CheckJulesConfig implementation
- [ ] E2E tests

### Phase 4: Polish + Documentation (Week 4)
- [ ] Comprehensive error handling
- [ ] Security review
- [ ] API documentation
- [ ] User guides
- [ ] >80% test coverage

## Security Considerations

### Token Storage
```csharp
// ✅ CORRECT: Environment variables
var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

// ❌ WRONG: Hardcoded
var token = "ghp_xxxxxxxxxxxx";
```

### Input Validation
```csharp
public static async Task<ReviewTriggerResult> TriggerAllReviewers(
    string owner, string repo, int prNumber)
{
    // Validate inputs
    ArgumentException.ThrowIfNullOrWhiteSpace(owner, nameof(owner));
    ArgumentException.ThrowIfNullOrWhiteSpace(repo, nameof(repo));
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prNumber, nameof(prNumber));

    // Proceed with API calls
    // ...
}
```

### Secrets Management
- ✅ Use environment variables only
- ❌ Never hardcode tokens
- ❌ Never commit tokens to Git
- ✅ Use Docker secrets in production

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task TriggerAllReviewers_CallsGitHubAPI()
{
    // Arrange
    var mockGitHub = new Mock<IGitHubClient>();
    mockGitHub.Setup(x => x.Issue.Comment.Create(
        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
        .ReturnsAsync(new IssueComment());

    // Act
    var result = await TriggerAllReviewers("owner", "repo", 1);

    // Assert
    Assert.Equal(4, result.TriggeredServices);
    mockGitHub.Verify(x => x.Issue.Comment.Create(
        "owner", "repo", 1, It.IsAny<string>()), Times.Exactly(4));
}
```

### Integration Tests
```csharp
[Fact]
public async Task GetCoverage_RealCodecovAPI()
{
    // Uses real API with test project
    var coverage = await GetCoverage("test-owner", "test-repo");
    Assert.InRange(coverage.OverallCoverage, 0, 100);
}
```

## Migration Path

### Tool Compatibility

**Key Principle**: Tool signatures remain unchanged between v1.0 and v2.0.

**v1.0 Signature**:
```csharp
public static Task<string> TriggerAllReviewers(string owner, string repo, int prNumber)
```

**v2.0 Signature**:
```csharp
public static async Task<ReviewTriggerResult> TriggerAllReviewers(string owner, string repo, int prNumber)
```

**Breaking Change**: Return type changes from `string` to `ReviewTriggerResult`.

**Migration Strategy**:
1. Version tools explicitly: `TriggerAllReviewers_v1`, `TriggerAllReviewers_v2`
2. Use semantic versioning: v1.0 vs v2.0
3. Deprecation period: Keep v1.0 tools for 1 release cycle

## Status

- **Status**: Planned
- **Target Version**: v2.0
- **Dependencies**:
  - ADR-001 (Instruction-Based Tools)
  - ADR-002 (Docker Registry Submission Timing)
  - docs/API_INTEGRATION.md
- **Estimated Effort**: 4 weeks
- **Target Completion**: 2025-03-15

## References

- [ADR-001: Instruction-Based Tools](../decisions/adr-001-instruction-based-tools.md)
- [ADR-002: Docker Registry Submission](../decisions/adr-002-docker-registry-submission.md)
- [API Integration Guide](../API_INTEGRATION.md)
- [GitHub API Documentation](https://docs.github.com/en/rest)
- [Octokit.NET](https://octokitnet.readthedocs.io/)
- [Codecov API](https://docs.codecov.com/reference)
