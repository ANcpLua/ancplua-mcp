# Jules AI PR Review Setup Guide

This guide explains how to configure automated pull request reviews using Google's Jules AI in GitHub Actions.

## Overview

The PR review workflow provides two modes of operation:

1. **Automatic Review**: Triggers automatically when a PR is opened, updated, or reopened
2. **On-Demand Review**: Triggers when you comment `/jules-review` on a PR

## Prerequisites

- GitHub repository with Actions enabled
- Jules API key from [jules.google.com](https://jules.google.com)
- Repository admin/maintainer permissions to configure secrets

## Setup Instructions

### Step 1: Obtain a Jules API Key

1. Visit [jules.google.com](https://jules.google.com)
2. Sign in with your Google account
3. Navigate to Settings → API Keys
4. Click "Create API Key"
5. Copy the generated API key (format: `AQ.xxxxxxxxxxxxx`)
6. Store it securely

### Step 2: Configure GitHub Repository Secret

1. Navigate to your GitHub repository
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Set the name to: `JULES_API_KEY`
5. Paste your API key as the value
6. Click **Add secret**

### Step 3: Verify Workflow File

The workflow file is configured at `.github/workflows/pr-review.yml`. It includes:

- **Action**: `BeksOmega/jules-action@v1`
- **Triggers**: PR events and `/jules-review` comments
- **Permissions**: Read contents, write PR comments

### Step 4: Test the Setup

#### Test Automatic Review

1. Create a new branch: `git checkout -b test/jules-review`
2. Make some code changes (e.g., modify a C# file)
3. Commit and push: `git commit -am "test: jules review" && git push -u origin test/jules-review`
4. Open a pull request
5. Wait for the workflow to run (check the **Actions** tab)
6. Jules should post a review comment within 2-5 minutes

#### Test On-Demand Review

1. Open any existing pull request
2. Add a comment with: `/jules-review`
3. The workflow will trigger and post a review

## Usage

### Automatic Reviews

Automatic reviews run on:
- Pull request opened
- Pull request synchronized (new commits pushed)
- Pull request reopened

No action needed—Jules reviews your changes automatically.

### On-Demand Reviews

To request a review at any time:

1. Comment `/jules-review` on the PR
2. Wait for the workflow to complete
3. Review the AI-generated feedback

### Customizing the Review

You can customize the workflow by editing `.github/workflows/pr-review.yml`:

#### Modify the Review Prompt

Update the `prompt` parameter in the workflow:

```yaml
- name: Review PR with Jules
  uses: BeksOmega/jules-action@v1
  with:
    prompt: |
      Review this pull request focusing on:
      - Security vulnerabilities
      - Performance optimizations
      - Code duplication
      - Error handling

      Provide specific code suggestions with line numbers.
    jules_api_key: ${{ secrets.JULES_API_KEY }}
```

#### Adjust Trigger Conditions

Modify the `on:` section to change when reviews run:

```yaml
on:
  pull_request:
    types: [opened]  # Only on open, not on updates
    branches: [ main ]  # Only for main branch
```

## Troubleshooting

### "Invalid API key" Error

**Cause**: Invalid or expired API key

**Solution**:
1. Generate a new API key at [jules.google.com](https://jules.google.com)
2. Update the `JULES_API_KEY` secret in GitHub
3. Re-run the workflow

### Workflow Not Triggering

**Cause**: Permissions or configuration issue

**Solution**:
1. Check that Actions are enabled: **Settings** → **Actions** → **General**
2. Verify the workflow file syntax is valid
3. Check workflow permissions include `pull-requests: write`

### Review Comments Not Posting

**Cause**: Insufficient permissions or action failure

**Solution**:
1. Verify `GITHUB_TOKEN` has write access to pull requests
2. Check GitHub Actions logs for specific errors
3. Ensure the Jules action is properly configured

### Workflow Takes Too Long

**Cause**: Large PR or complex analysis

**Solution**:
1. Review smaller PRs
2. Use on-demand reviews instead of automatic
3. Adjust the prompt to focus on specific concerns

## Cost and Rate Limits

### Jules Pricing

- **Free Tier**: Limited requests per month
- **Pro Tier**: Higher rate limits and priority processing
- Check [jules.google.com](https://jules.google.com) for current pricing

### Rate Limits

- Free tier: ~50 requests per month
- Pro tier: Higher limits based on subscription
- Enterprise: Custom limits available

## Security Best Practices

1. **Never commit API keys** to the repository
2. **Use repository secrets** for all credentials
3. **Rotate API keys** periodically (every 90 days recommended)
4. **Monitor usage** in Jules dashboard
5. **Set up notifications** for unusual activity

## Advanced Configuration

### Multiple Review Types

You can create separate workflows for different review types:

**Security Review** (`.github/workflows/security-review.yml`):
```yaml
name: Security Review
on:
  pull_request:
    paths:
      - 'src/**/*.cs'
      - 'src/**/*.csproj'

jobs:
  security:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v6
    - uses: BeksOmega/jules-action@v1
      with:
        prompt: |
          Perform a security-focused review:
          - SQL injection vulnerabilities
          - XSS vulnerabilities
          - Authentication/authorization issues
          - Sensitive data exposure
        jules_api_key: ${{ secrets.JULES_API_KEY }}
```

### Integration with Branch Protection

To require Jules reviews before merging:

1. Go to **Settings** → **Branches**
2. Add a branch protection rule for `main`
3. Enable "Require status checks to pass before merging"
4. Select the Jules review workflow

### Custom Workflow Triggers

Trigger Jules on specific file changes:

```yaml
on:
  pull_request:
    paths:
      - 'src/**/*.cs'
      - '!src/**/Tests/**'
```

## Comparison: Jules vs Gemini Code Assist

| Feature | Jules | Gemini Code Assist |
|---------|-------|-------------------|
| Setup | GitHub Action | GitHub App |
| API Key | Required (from Jules) | Not required |
| Customization | Full prompt control | Limited customization |
| Code Generation | Yes | Review only |
| PR Creation | Can create PRs | Reviews only |
| Cost | Subscription-based | Free/Pay-per-use |

## Resources

- [Jules Home](https://jules.google.com) - Get API keys and documentation
- [Jules Action](https://github.com/marketplace/actions/jules-action) - GitHub Action details
- [GitHub Actions Documentation](https://docs.github.com/en/actions) - Workflow syntax

## Support

For issues with:
- **This workflow**: Open an issue in this repository
- **The GitHub Action**: Visit [BeksOmega/jules-action](https://github.com/BeksOmega/jules-action)
- **Jules API**: Check [jules.google.com](https://jules.google.com) support

---

**Last Updated**: November 2025
**Action Version**: v1
