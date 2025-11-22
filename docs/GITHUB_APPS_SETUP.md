# GitHub Apps Setup Guide

This document explains the GitHub Apps configured for the `ancplua-mcp` repository and how to use them.

## Installed Apps Summary

### PR Review & Code Quality
1. **Gemini Code Assist** - Free, automatic PR reviews
2. **CodeRabbit AI** - Free for open source, deep code analysis
3. **Jules AI** - On-demand custom reviews via GitHub Actions

### Security
1. **GitGuardian** - Secret scanning with UI dashboard
2. **TruffleHog** - Secret scanning in CI (workflow-based)
3. **CodeQL** - Security vulnerability analysis (workflow-based)

### Testing & Coverage
1. **Codecov** - Code coverage tracking and reporting
2. **Codecov AI** - AI-powered test generation and suggestions

### Automation
1. **Renovate** - Smart dependency updates
2. **Mergify** - Auto-merge and PR queue management

### Development Tools
1. **GitHub Official MCP Server** - GitHub API integration for MCP clients
2. **WakaTime** - Development time tracking

## Configuration Files

### `.gemini/code-review-guide.md`
Customizes Gemini Code Assist reviews to focus on:
- MCP protocol compliance
- C# and .NET 10 best practices
- Security (OWASP Top 10)
- Testing requirements
- Architecture alignment

### `renovate.json`
Configures Renovate to:
- Group .NET and MCP SDK dependencies
- Auto-merge minor/patch updates that pass CI
- Schedule updates for Monday mornings
- Require manual review for security and MCP SDK updates
- Create dependency dashboard

### `.mergify.yml`
Configures Mergify to:
- Auto-merge Renovate PRs that pass CI
- Auto-approve safe dependency updates
- Request manual review for security/MCP updates
- Auto-label PRs based on changed files
- Keep PRs updated with main branch
- Close stale PRs after 90 days

### `codecov.yml`
Configures Codecov to:
- Target 80% project coverage, 70% patch coverage
- Track coverage trends
- Comment on PRs with coverage reports
- Ignore test files and generated code

## Using the Apps

### Gemini Code Assist

**Automatic**: Reviews all PRs within 5 minutes of opening.

**Commands** in PR comments:
```
/gemini review    - Request full review
/gemini summary   - Get PR summary
/gemini help      - Show available commands
```

**Customization**: Edit `.gemini/code-review-guide.md` to adjust review focus.

### CodeRabbit AI

**Automatic**: Reviews PRs with inline suggestions.

**Features**:
- Click-to-commit suggestions
- Chat interface for questions
- Issue validation against code changes
- Multi-language support

**Commands** in PR comments:
```
@coderabbitai review   - Request review
@coderabbitai chat     - Ask questions about code
```

### Jules AI

**Automatic**: Reviews PRs opened/updated on main or develop branches.

**On-Demand**: Comment `/jules-review` on any PR.

**Workflow**: See `.github/workflows/pr-review.yml`

### Codecov AI

**Commands** in PR comments:
```
@codecov-ai-reviewer review   - Review code changes
@codecov-ai-reviewer test     - Generate tests for uncovered code
```

**Note**: Sends code diffs to VertexAI (Google Cloud).

### Renovate

**Automatic**: Scans for dependency updates every Monday morning.

**Features**:
- Dependency dashboard (GitHub issue)
- Grouped updates (e.g., all .NET packages together)
- Auto-merge safe updates after CI passes
- Security alerts get manual review

**Dashboard**: Check Issues tab for "Dependency Dashboard" issue.

### Mergify

**Automatic**: Manages PR merge queue and auto-merge.

**Features**:
- Speculative checks (tests multiple PRs in parallel)
- Batch merging
- Auto-approve safe Renovate PRs
- Auto-label based on changed files

**Queue Status**: Check PR labels for `in-merge-queue`.

### GitHub Official MCP Server

**Purpose**: Provides GitHub API integration for MCP clients (Claude Desktop, etc.)

**40 Tools** including:
- PR operations (create, update, merge, review)
- Issue management (create, comment, assign)
- Repository operations (fork, create, search)
- File operations (create, update, delete via GitHub API)
- Copilot integration (assign to issues, request reviews)

**Setup**: Use with Claude Desktop or other MCP clients. Requires GitHub Personal Access Token.

**Configuration Example** for Claude Desktop:
```json
{
  "mcpServers": {
    "github-official": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "GITHUB_PERSONAL_ACCESS_TOKEN",
        "ghcr.io/github/github-mcp-server"
      ],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "<YOUR_TOKEN>"
      }
    }
  }
}
```

**Difference from Your Servers**:
- **Your MCP servers**: Local filesystem, git operations, CI commands
- **GitHub Official MCP**: Remote GitHub API operations, PRs, issues

They **complement each other**!

## Required Secrets

Add these secrets to your repository (Settings → Secrets and variables → Actions):

### Docker Hub
- `DOCKERHUB_USERNAME` - Your Docker Hub username
- `DOCKERHUB_TOKEN` - Docker Hub access token

### Jules AI
- `JULES_API_KEY` - Jules API key from [jules.google.com](https://jules.google.com)

### GitHub Official MCP Server
- `GITHUB_PERSONAL_ACCESS_TOKEN` - GitHub PAT with repo permissions

## CI/CD Workflows

### `.github/workflows/ci.yml`
- Builds and tests code
- Runs CodeQL security analysis
- Performs secret scanning (TruffleHog)
- Uploads coverage to Codecov
- Dependency review on PRs

### `.github/workflows/pr-review.yml`
- Automatic PR reviews with Jules AI
- On-demand reviews via `/jules-review` command

### `.github/workflows/docker-build-push.yml`
- Builds Docker images for both servers
- Pushes to Docker Hub on main branch and tags
- Multi-platform builds (AMD64, ARM64)

## Best Practices

### For PR Reviews
1. Let **Gemini** and **CodeRabbit** review automatically first
2. Use `/jules-review` for custom deep-dives
3. Address AI feedback before requesting human review

### For Dependencies
1. Check **Dependency Dashboard** issue weekly
2. Let **Renovate** auto-merge safe updates
3. Manually review security and MCP SDK updates
4. Monitor **Mergify** merge queue for conflicts

### For Coverage
1. Ensure all new code has tests
2. Check **Codecov** comments on PRs
3. Use `@codecov-ai-reviewer test` to generate missing tests
4. Maintain 80% project coverage target

### For Security
1. Fix **GitGuardian** alerts immediately
2. Review **CodeQL** findings in Security tab
3. Address **TruffleHog** failures in CI
4. Rotate secrets if exposed

## Troubleshooting

### App not working
1. Check Settings → Integrations → Applications
2. Verify app has necessary permissions
3. Check app is installed for the repository

### Secrets not found
1. Go to Settings → Secrets and variables → Actions
2. Add missing secrets
3. Ensure secret names match exactly

### Renovate not creating PRs
1. Check Dependency Dashboard issue
2. Look for configuration errors
3. Verify `renovate.json` syntax

### Mergify not auto-merging
1. Verify all required checks pass
2. Check `.mergify.yml` conditions
3. Look for merge conflicts

## Further Reading

- [Gemini Code Assist Docs](https://cloud.google.com/gemini/docs/codebase/code-assist)
- [CodeRabbit Documentation](https://docs.coderabbit.ai/)
- [Jules AI Setup](./jules-pr-review-setup.md)
- [Renovate Documentation](https://docs.renovatebot.com/)
- [Mergify Documentation](https://docs.mergify.com/)
- [Codecov Documentation](https://docs.codecov.com/)
- [GitHub Official MCP Server](https://github.com/github/github-mcp-server)
- [Docker Deployment](./DOCKER.md)
