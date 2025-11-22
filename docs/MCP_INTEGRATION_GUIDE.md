# MCP Integration Guide: Unified GitHub Apps Communication

This guide shows how to configure Claude Code (or other MCP clients) to communicate with all your GitHub Apps through Model Context Protocol servers.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                   Claude / MCP Client                    │
└───────────────────────┬─────────────────────────────────┘
                        │ MCP Protocol
        ┌───────────────┼───────────────┐
        │               │               │
┌───────▼─────┐  ┌──────▼──────┐  ┌────▼────────────┐
│   GitHub    │  │  ancplua-   │  │ ancplua-github  │
│  Official   │  │     mcp     │  │   -apps (NEW)   │
│ MCP Server  │  │  Workstation│  │                 │
└─────┬───────┘  └──────┬──────┘  └────┬────────────┘
      │                 │              │
      │ GitHub API      │ Local Ops    │ App APIs
      ▼                 ▼              ▼
┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│   GitHub    │   │ Filesystem  │   │ Gemini      │
│   PRs       │   │    Git      │   │ CodeRabbit  │
│   Issues    │   │    CI/CD    │   │ Jules       │
│   Repos     │   │             │   │ Codecov     │
│             │   │             │   │ GitGuardian │
│             │   │             │   │ Mergify     │
│             │   │             │   │ Renovate    │
└─────────────┘   └─────────────┘   └─────────────┘
```

## MCP Servers You'll Use

### 1. GitHub Official MCP Server
**Purpose**: GitHub API operations (PRs, issues, repos, files)
**40 Tools** for comprehensive GitHub automation

### 2. ancplua-mcp (Your Servers)
**Purpose**: Local development operations
**Tools**: Filesystem, Git, CI/CD commands

### 3. ancplua-github-apps (Proposed)
**Purpose**: GitHub Apps integration
**Tools**: Gemini, CodeRabbit, Jules, Codecov, GitGuardian, Mergify, Renovate

## Complete MCP Configuration

### For Claude Desktop

Create or update `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS):

```json
{
  "mcpServers": {
    "github-official": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-e",
        "GITHUB_PERSONAL_ACCESS_TOKEN",
        "ghcr.io/github/github-mcp-server"
      ],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "ghp_YOUR_TOKEN_HERE"
      }
    },
    "ancplua-workstation": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/ancplua/ancplua-mcp/src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj"
      ]
    },
    "ancplua-github-apps": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/ancplua/ancplua-mcp/src/Ancplua.Mcp.GitHubAppsServer/Ancplua.Mcp.GitHubAppsServer.csproj"
      ],
      "env": {
        "GITHUB_TOKEN": "ghp_YOUR_TOKEN_HERE",
        "GEMINI_API_KEY": "your-gemini-api-key",
        "JULES_API_KEY": "your-jules-api-key",
        "CODECOV_TOKEN": "your-codecov-token",
        "GITGUARDIAN_API_KEY": "your-gitguardian-api-key"
      }
    }
  }
}
```

### For GitHub Copilot

Add to repository settings → Copilot → MCP configuration:

```json
{
  "mcpServers": {
    "github-official": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-e",
        "GITHUB_PERSONAL_ACCESS_TOKEN",
        "ghcr.io/github/github-mcp-server"
      ],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "${GITHUB_TOKEN}"
      }
    },
    "ancplua-workstation": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/ancplua/ancplua-mcp/src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj"
      ]
    }
  }
}
```

## Required API Keys & Tokens

### 1. GitHub Personal Access Token
**Scope**: `repo`, `read:org`, `write:discussion`

Generate at: https://github.com/settings/tokens/new

```bash
export GITHUB_TOKEN=ghp_your_token_here
export GITHUB_PERSONAL_ACCESS_TOKEN=$GITHUB_TOKEN
```

### 2. Gemini API Key
Get from: https://makersuite.google.com/app/apikey

```bash
export GEMINI_API_KEY=your-gemini-api-key
```

### 3. Jules API Key
Get from: https://jules.google.com

```bash
export JULES_API_KEY=your-jules-api-key
```

### 4. Codecov Token
Get from: https://codecov.io/gh/ANcpLua/ancplua-mcp/settings

```bash
export CODECOV_TOKEN=your-codecov-upload-token
```

### 5. GitGuardian API Key
Get from: https://dashboard.gitguardian.com/

```bash
export GITGUARDIAN_API_KEY=your-gitguardian-api-key
```

## How Claude Will Use These Servers

### Example 1: Comprehensive PR Review

**User**: "Review PR #42 comprehensively"

**Claude executes**:
```
1. Uses github-official MCP: get_pull_request(42)
2. Uses ancplua-workstation: git_get_diff(PR branch)
3. Uses ancplua-github-apps:
   - gemini_request_review(42)
   - coderabbit_request_review(42)
   - jules_trigger_review(42)
   - gitguardian_scan_pr(42)
   - codecov_get_coverage(42)
4. Aggregates all feedback
5. Presents unified review
```

### Example 2: Security Audit

**User**: "Security audit PR #42"

**Claude executes**:
```
1. Uses github-official: get_pull_request(42)
2. Uses ancplua-github-apps:
   - gitguardian_scan_pr(42) → Check for secrets
   - codeql_get_results(42) → Get security analysis
   - coderabbit_request_review(42, focus="security")
   - gemini_request_review(42, focus="OWASP")
3. Generates security report
4. Uses github-official: add_issue_comment(42, report)
```

### Example 3: Auto-Merge Safe Dependencies

**User**: "Approve and merge safe Renovate PRs"

**Claude executes**:
```
1. Uses ancplua-github-apps: renovate_get_prs()
2. For each PR:
   - Uses github-official: get_pull_request(PR_NUMBER)
   - Check CI status
   - If minor/patch update + CI passing:
     - Uses ancplua-github-apps: mergify_add_to_queue(PR_NUMBER)
3. Reports approved PRs
```

### Example 4: Generate Missing Tests

**User**: "Generate tests for uncovered code in PR #42"

**Claude executes**:
```
1. Uses ancplua-github-apps: codecov_get_coverage(42)
2. Identifies files below 80% coverage
3. Uses ancplua-github-apps: codecov_ai_generate_tests(42, files)
4. Uses ancplua-workstation: write_file(test_file, generated_tests)
5. Uses github-official: push_files(branch, test_files)
6. Reports results
```

## Tool Mapping: Which Server Has What

### GitHub Operations
| Operation | Server | Tool |
|-----------|--------|------|
| Get PR details | github-official | `pull_request_read` |
| Create PR | github-official | `create_pull_request` |
| Merge PR | github-official | `merge_pull_request` |
| Add PR comment | github-official | `add_issue_comment` |
| Get file contents | github-official | `get_file_contents` |
| Create/update file | github-official | `create_or_update_file` |
| Search code | github-official | `search_code` |
| Create issue | github-official | `issue_write` |

### Local Operations
| Operation | Server | Tool |
|-----------|--------|------|
| Read local file | ancplua-workstation | `FileSystemTools.ReadFile` |
| Write local file | ancplua-workstation | `FileSystemTools.WriteFile` |
| List directory | ancplua-workstation | `FileSystemTools.ListDirectory` |
| Git status | ancplua-workstation | `GitTools.GetStatus` |
| Git diff | ancplua-workstation | `GitTools.GetDiff` |
| Git commit | ancplua-workstation | `GitTools.Commit` |
| Dotnet build | ancplua-workstation | `CiTools.Build` |
| Dotnet test | ancplua-workstation | `CiTools.Test` |

### GitHub Apps Integration
| Operation | Server | Tool |
|-----------|--------|------|
| Request Gemini review | ancplua-github-apps | `gemini_request_review` |
| Request CodeRabbit review | ancplua-github-apps | `coderabbit_request_review` |
| Trigger Jules review | ancplua-github-apps | `jules_trigger_review` |
| Scan for secrets | ancplua-github-apps | `gitguardian_scan_pr` |
| Get CodeQL results | ancplua-github-apps | `codeql_get_results` |
| Get coverage report | ancplua-github-apps | `codecov_get_coverage` |
| Generate tests (AI) | ancplua-github-apps | `codecov_ai_generate_tests` |
| Get Renovate PRs | ancplua-github-apps | `renovate_get_prs` |
| Add to merge queue | ancplua-github-apps | `mergify_add_to_queue` |

## Testing Your Setup

### 1. Test GitHub Official MCP Server

```bash
# In Claude Desktop or terminal with MCP client
claude> List my recent PRs in ancplua-mcp
```

Expected: Claude uses `github-official` → `list_pull_requests` → Shows your PRs

### 2. Test ancplua-workstation

```bash
claude> Show me the git status
```

Expected: Claude uses `ancplua-workstation` → `GitTools.GetStatus` → Shows git status

### 3. Test ancplua-github-apps (When Implemented)

```bash
claude> Request Gemini review for PR #42
```

Expected: Claude uses `ancplua-github-apps` → `gemini_request_review` → Triggers review

## Next Steps

### Phase 1: Set Up Existing Servers ✅
1. ✅ Configure GitHub Official MCP Server
2. ✅ Configure ancplua-workstation
3. Test integration with Claude Desktop

### Phase 2: Implement GitHub Apps MCP Server (Coming Soon)
1. Create `src/Ancplua.Mcp.GitHubAppsServer/` project
2. Implement review app tools (Gemini, CodeRabbit, Jules)
3. Implement security tools (GitGuardian, CodeQL)
4. Implement testing tools (Codecov, Codecov AI)
5. Implement automation tools (Renovate, Mergify)

### Phase 3: Advanced Workflows (Future)
1. Create pre-built workflow tools
2. Add webhook support for real-time updates
3. Implement caching for performance
4. Add analytics and reporting

## Troubleshooting

### GitHub Official MCP Server Not Working

**Problem**: `command not found: docker`
**Solution**: Install Docker Desktop or use npm installation:
```bash
npm install -g @github/github-mcp-server
```

**Problem**: `Authentication failed`
**Solution**: Check your GITHUB_PERSONAL_ACCESS_TOKEN has correct permissions

### ancplua-workstation Not Working

**Problem**: `dotnet: command not found`
**Solution**: Install .NET 10.0 SDK from https://dot.net

**Problem**: `Project file not found`
**Solution**: Update file path in MCP config to absolute path:
```bash
pwd  # Get current directory
# Update path in config to /full/path/to/ancplua-mcp/src/...
```

### API Rate Limiting

GitHub API has rate limits:
- **Authenticated**: 5,000 requests/hour
- **Unauthenticated**: 60 requests/hour

**Solution**: Ensure GITHUB_TOKEN is set to get higher limits.

## Security Best Practices

1. **Never commit API keys** - Use environment variables
2. **Rotate tokens regularly** - Especially after public exposure
3. **Use minimal permissions** - Only grant necessary scopes
4. **Monitor API usage** - Check for unexpected spikes
5. **Enable 2FA** - On all accounts providing API keys

## Resources

- [GitHub Official MCP Server](https://github.com/github/github-mcp-server)
- [Model Context Protocol Docs](https://modelcontextprotocol.io/)
- [Claude Code Documentation](https://docs.anthropic.com/claude/docs)
- [GitHub Apps Spec](./specs/spec-github-apps-integration.md)
- [Docker Deployment](./DOCKER.md)
