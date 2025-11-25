# Specification: GitHub Apps Integration MCP Server

**Status**: Proposed
**Created**: 2025-11-22
**Last Updated**: 2025-11-22

## Problem Statement

The ancplua-mcp repository uses 13+ GitHub Apps for PR reviews, security, testing, and automation. Currently:
- Each app operates independently
- No unified interface for AI assistants (Claude, Jules, Gemini, etc.) to interact with all apps
- Manual context switching between different app interfaces
- No programmatic way to orchestrate multi-app workflows

## Proposed Solution

Create a **GitHub Apps Integration MCP Server** that provides unified MCP tools for interacting with all installed GitHub Apps through their APIs.

## Value Proposition

1. **Unified Interface**: Single MCP server for all GitHub App interactions
2. **AI Orchestration**: Enable Claude/Jules to coordinate between apps
3. **Workflow Automation**: Create complex multi-app workflows programmatically
4. **Centralized Management**: Monitor and control all apps from one place

## Architecture

### New Server: `Ancplua.Mcp.GitHubAppsServer`

Location: `src/Ancplua.Mcp.GitHubAppsServer/`

### Tool Groups

#### 1. PR Review Tools
- **Gemini Code Assist**
  - `gemini_request_review` - Request Gemini review
  - `gemini_get_review_status` - Check review status

- **CodeRabbit AI**
  - `coderabbit_request_review` - Request CodeRabbit review
  - `coderabbit_chat` - Ask CodeRabbit questions

- **Jules AI**
  - `jules_trigger_review` - Trigger Jules workflow
  - `jules_check_status` - Check Jules review status

#### 2. Security Tools
- **GitGuardian**
  - `gitguardian_scan_pr` - Scan PR for secrets
  - `gitguardian_get_alerts` - Get secret alerts

- **CodeQL**
  - `codeql_get_results` - Get CodeQL analysis results
  - `codeql_get_alerts` - Get security alerts

#### 3. Testing & Coverage Tools
- **Codecov**
  - `codecov_get_coverage` - Get coverage report
  - `codecov_compare_pr` - Compare PR coverage vs base

- **Codecov AI**
  - `codecov_ai_review` - Request Codecov AI review
  - `codecov_ai_generate_tests` - Generate tests for uncovered code

#### 4. Automation Tools
- **Renovate**
  - `renovate_get_dashboard` - Get dependency dashboard
  - `renovate_get_prs` - List Renovate PRs
  - `renovate_approve_pr` - Approve dependency PR

- **Mergify**
  - `mergify_get_queue` - Get merge queue status
  - `mergify_add_to_queue` - Add PR to merge queue
  - `mergify_check_rules` - Validate PR against Mergify rules

#### 5. AI Code Assistants
- **Claude**
  - `claude_ask` - Ask Claude questions
  - `claude_review_pr` - Request Claude PR review

- **ChatGPT Codex**
  - `chatgpt_ask` - Ask ChatGPT questions
  - `chatgpt_generate` - Generate code with ChatGPT

#### 6. GitHub Official
- Leverage existing **GitHub Official MCP Server** for:
  - PR operations
  - Issue management
  - Repository operations
  - File operations

## Tool Signatures

### Example: Request Multi-App Review

```csharp
[McpServerTool]
[Description("Request reviews from multiple AI reviewers (Gemini, CodeRabbit, Jules, Claude)")]
public static async Task<string> RequestMultiReview(
    [Description("PR number")] int prNumber,
    [Description("Reviewers to request (comma-separated: gemini,coderabbit,jules,claude)")] string reviewers,
    [Description("Review focus (e.g., security, performance, testing)")] string? focus = null)
{
    // Coordinate requests to multiple review apps
    // Return combined status
}
```

### Example: Orchestrate PR Workflow

```csharp
[McpServerTool]
[Description("Orchestrate complete PR workflow: security scan → tests → reviews → merge")]
public static async Task<string> OrchestratePRWorkflow(
    [Description("PR number")] int prNumber,
    [Description("Auto-merge if all checks pass")] bool autoMerge = false)
{
    // 1. GitGuardian secret scan
    // 2. CodeQL security analysis
    // 3. Codecov coverage check
    // 4. Request AI reviews (Gemini, CodeRabbit, Jules)
    // 5. Add to Mergify queue if all pass
    // Return workflow status
}
```

### Example: Generate Tests with AI

```csharp
[McpServerTool]
[Description("Generate tests for uncovered code using Codecov AI")]
public static async Task<string> GenerateTestsForUncovered(
    [Description("PR number")] int prNumber,
    [Description("Minimum coverage threshold (default: 80%)")] int threshold = 80)
{
    // 1. Get Codecov coverage report
    // 2. Identify uncovered code
    // 3. Request Codecov AI to generate tests
    // 4. Create PR with generated tests
    // Return results
}
```

## API Integration Strategy

### Option 1: Direct API Calls
- Use GitHub Apps' REST/GraphQL APIs directly
- Requires API tokens/credentials for each app
- Maximum control and flexibility

### Option 2: GitHub CLI Wrapper
- Use `gh` CLI for GitHub operations
- Leverage existing authentication
- Simpler implementation

### Option 3: Hybrid Approach (Recommended)
- Use **GitHub Official MCP Server** for core GitHub operations
- Direct API calls for app-specific features
- Fall back to `gh` CLI when needed

## Configuration

### Environment Variables

```bash
# GitHub
GITHUB_TOKEN=<GitHub PAT with repo permissions>

# Gemini Code Assist
GEMINI_API_KEY=<API key>

# CodeRabbit AI (uses GitHub App auth)
CODERABBIT_WEBHOOK_SECRET=<Optional webhook secret>

# Jules AI
JULES_API_KEY=<API key>

# Codecov
CODECOV_TOKEN=<Upload token>

# GitGuardian
GITGUARDIAN_API_KEY=<API key>
```

### MCP Configuration

```json
{
  "mcpServers": {
    "github-official": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "-e", "GITHUB_PERSONAL_ACCESS_TOKEN",
               "ghcr.io/github/github-mcp-server"],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_TOKEN}"
      }
    },
    "ancplua-github-apps": {
      "command": "dotnet",
      "args": ["run", "--project",
               "src/Ancplua.Mcp.GitHubAppsServer/Ancplua.Mcp.GitHubAppsServer.csproj"],
      "env": {
        "GITHUB_TOKEN": "${GITHUB_TOKEN}",
        "GEMINI_API_KEY": "${GEMINI_API_KEY}",
        "JULES_API_KEY": "${JULES_API_KEY}",
        "CODECOV_TOKEN": "${CODECOV_TOKEN}",
        "GITGUARDIAN_API_KEY": "${GITGUARDIAN_API_KEY}"
      }
    }
  }
}
```

## Example Use Cases

### Use Case 1: Comprehensive PR Review
```
Claude: "Review PR #42 comprehensively"

System executes:
1. Request Gemini review (code quality, best practices)
2. Request CodeRabbit review (bugs, security, performance)
3. Request Jules review (custom focus: MCP compliance)
4. Run GitGuardian secret scan
5. Check Codecov coverage delta
6. Aggregate all feedback
7. Present unified review to user
```

### Use Case 2: Security-Focused Review
```
Claude: "Security audit PR #42"

System executes:
1. GitGuardian secret scan
2. CodeQL security analysis
3. CodeRabbit security-focused review
4. Gemini review with security checklist
5. Generate security report
6. Block merge if critical issues found
```

### Use Case 3: Auto-Fix Coverage Gaps
```
Claude: "Fix coverage gaps in PR #42"

System executes:
1. Get Codecov report
2. Identify files below 80% coverage
3. Use Codecov AI to generate tests
4. Create new commit with tests
5. Re-run coverage
6. Report results
```

### Use Case 4: Dependency Update Management
```
Claude: "Approve safe Renovate PRs"

System executes:
1. Get Renovate dashboard
2. Filter for minor/patch updates
3. Check CI status for each PR
4. Auto-approve PRs that pass all checks
5. Add to Mergify queue
6. Report approved PRs
```

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- [ ] Create `Ancplua.Mcp.GitHubAppsServer` project
- [ ] Implement GitHub API client
- [ ] Add basic authentication
- [ ] Create test suite

### Phase 2: GitHub Official Integration (Week 1)
- [ ] Integrate GitHub Official MCP Server
- [ ] Implement PR/issue operations
- [ ] Add repository operations
- [ ] Test core workflows

### Phase 3: Review Apps (Week 2)
- [ ] Implement Gemini Code Assist tools
- [ ] Implement CodeRabbit AI tools
- [ ] Implement Jules AI tools
- [ ] Add multi-review orchestration

### Phase 4: Security Apps (Week 2)
- [ ] Implement GitGuardian tools
- [ ] Implement CodeQL tools
- [ ] Add security workflow orchestration

### Phase 5: Testing & Coverage (Week 3)
- [ ] Implement Codecov tools
- [ ] Implement Codecov AI tools
- [ ] Add test generation workflows

### Phase 6: Automation (Week 3)
- [ ] Implement Renovate tools
- [ ] Implement Mergify tools
- [ ] Add dependency workflow orchestration

### Phase 7: AI Assistants (Week 4)
- [ ] Implement Claude tools
- [ ] Implement ChatGPT Codex tools
- [ ] Add AI collaboration workflows

### Phase 8: Documentation & Polish (Week 4)
- [ ] Write comprehensive documentation
- [ ] Add examples and tutorials
- [ ] Create video demos
- [ ] Publish to Docker Hub

## Security Considerations

1. **API Key Management**: Store keys in environment variables, never in code
2. **Least Privilege**: Request minimum necessary permissions
3. **Token Rotation**: Support key rotation without downtime
4. **Audit Logging**: Log all API calls for security review
5. **Rate Limiting**: Respect GitHub API rate limits (5000 req/hour)

## Security & Compliance

### Jules Workflow Security (CRITICAL)

Analysis of existing Jules workflows (`jules-auto-reviewer.yml`, `jules-cleanup.yml`) has identified critical security risks that this server must mitigate:

*   **Risk**: Unsupervised Auto-Merge.
    *   *Mitigation*: This server MUST NOT trigger auto-merge workflows directly. It should only trigger "Review Only" modes.
*   **Risk**: CI Bypass.
    *   *Mitigation*: All AI-triggered changes must go through standard PRs with full status checks.

### Recommended GitHub Ruleset

To safely use these AI tools, the target repository should enforce a ruleset similar to:

```json
{
  "name": "Copilot review for default branch",
  "target": "branch",
  "enforcement": "active",
  "rules": [
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 2,
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": true
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "required_status_checks": [
          { "context": "build-and-test" },
          { "context": "code-quality" },
          { "context": "codeql / Analyze (csharp)" },
          { "context": "secret-scan" },
          { "context": "security-scan" }
        ]
      }
    }
  ]
}
```

## Testing Strategy

### Unit Tests
- Test each tool in isolation
- Mock API responses
- Verify error handling

### Integration Tests
- Test against real GitHub APIs (test repo)
- Verify multi-app orchestration
- Test rate limiting behavior

### End-to-End Tests
- Test complete workflows
- Verify Claude/Jules integration
- Test error recovery

## Success Metrics

1. **API Coverage**: 90%+ of app features accessible via MCP tools
2. **Response Time**: <2s for simple operations, <10s for complex workflows
3. **Reliability**: 99.9% uptime, graceful degradation on API failures
4. **Adoption**: Used in 80%+ of PR workflows within 1 month

## Open Questions

1. Should we support webhook-based real-time updates?
2. How to handle API rate limiting across multiple apps?
3. Should we cache API responses for performance?
4. How to handle authentication for different app types?

## References

- [GitHub Apps Documentation](https://docs.github.com/en/apps)
- [GitHub Official MCP Server](https://github.com/github/github-mcp-server)
- [Gemini Code Assist API](https://cloud.google.com/gemini/docs/codebase/code-assist)
- [CodeRabbit API](https://docs.coderabbit.ai/api)
- [Codecov API](https://docs.codecov.com/reference)
- [GitGuardian API](https://api.gitguardian.com/)
- [Renovate API](https://docs.renovatebot.com/configuration-options/)
- [Mergify API](https://docs.mergify.com/api/)
