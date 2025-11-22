# Gemini 3.0 Pro PR Review Setup Guide

This guide explains how to configure automated pull request reviews using Google's Gemini 3.0 Pro AI model in GitHub Actions.

## Overview

The PR review workflow provides two modes of operation:

1. **Automatic Review**: Triggers automatically when a PR is opened, updated, or reopened
2. **On-Demand Review**: Triggers when you comment `/gemini-review` on a PR

## Prerequisites

- GitHub repository with Actions enabled
- Access to Google AI Studio or Google Cloud for API keys
- Repository admin/maintainer permissions to configure secrets

## Setup Instructions

### Step 1: Obtain a Gemini API Key

Choose one of these methods:

#### Option A: Google AI Studio (Recommended for Personal/Small Projects)

1. Visit [Google AI Studio](https://aistudio.google.com/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the generated API key (you won't be able to see it again)
5. Store it securely

#### Option B: Google Cloud (Recommended for Enterprise)

1. Create or select a Google Cloud project
2. Enable the Gemini API
3. Create service account credentials
4. Generate an API key or use Workload Identity Federation (more secure)

### Step 2: Configure GitHub Repository Secret

1. Navigate to your GitHub repository
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Set the name to: `GEMINI_API_KEY`
5. Paste your API key as the value
6. Click **Add secret**

### Step 3: Verify Workflow File

The workflow file is already configured at `.github/workflows/pr-review.yml`. It includes:

- **Model**: Gemini 3.0 Pro (`gemini-3.0-pro`)
- **Excluded files**: Documentation, configs, and lock files
- **Permissions**: Read contents, write PR comments

### Step 4: Test the Setup

#### Test Automatic Review

1. Create a new branch: `git checkout -b test/gemini-review`
2. Make some code changes (e.g., modify a C# file)
3. Commit and push: `git commit -am "test: gemini review" && git push -u origin test/gemini-review`
4. Open a pull request
5. Wait for the workflow to run (check the **Actions** tab)
6. Gemini should post a review comment within 1-2 minutes

#### Test On-Demand Review

1. Open any existing pull request
2. Add a comment with: `/gemini-review`
3. The workflow will trigger and post a review

## Usage

### Automatic Reviews

Automatic reviews run on:
- Pull request opened
- Pull request synchronized (new commits pushed)
- Pull request reopened

No action needed—Gemini reviews your changes automatically.

### On-Demand Reviews

To request a review at any time:

1. Comment `/gemini-review` on the PR
2. Wait for the workflow to complete
3. Review the AI-generated feedback

### Customizing the Review

You can customize the workflow by editing `.github/workflows/pr-review.yml`:

#### Change the Model

Replace `gemini-3.0-pro` with another model:
```yaml
GEMINI_MODEL: "gemini-2.5-flash"  # Faster, cheaper
GEMINI_MODEL: "gemini-3.0-pro"    # Current default
```

#### Modify Excluded Files

Update the `EXCLUDE` parameter:
```yaml
EXCLUDE: "*.md,*.txt,dist/**,*.lock"
```

Common patterns:
- `*.md` - Markdown files
- `*.json` - JSON files (configs, package files)
- `dist/**` - Distribution/build folders
- `**/tests/**` - Test files
- `*.generated.cs` - Generated code

#### Adjust Trigger Conditions

Modify the `on:` section to change when reviews run:

```yaml
on:
  pull_request:
    types: [opened]  # Only on open, not on updates
    branches: [ main ]  # Only for main branch
```

## Troubleshooting

### "API key not valid" Error

**Cause**: Invalid or expired API key

**Solution**:
1. Generate a new API key at [Google AI Studio](https://aistudio.google.com/apikey)
2. Update the `GEMINI_API_KEY` secret in GitHub
3. Re-run the workflow

### Workflow Not Triggering

**Cause**: Permissions or configuration issue

**Solution**:
1. Check that Actions are enabled: **Settings** → **Actions** → **General**
2. Verify the workflow file syntax is valid
3. Check workflow permissions include `pull-requests: write`

### Review Comments Not Posting

**Cause**: Insufficient permissions or rate limiting

**Solution**:
1. Verify `GITHUB_TOKEN` has write access to pull requests
2. Check GitHub Actions logs for specific errors
3. Ensure you haven't exceeded API rate limits (check Google Cloud Console)

### "Model not found" Error

**Cause**: Specified model not available with your API key

**Solution**:
1. Verify your API key has access to Gemini 3.0 Pro
2. Try falling back to `gemini-2.5-flash`
3. Check [Google AI models documentation](https://ai.google.dev/models) for available models

### Workflow Takes Too Long

**Cause**: Large PR or slow API response

**Solution**:
1. Use `gemini-2.5-flash` for faster reviews
2. Exclude more files (builds, tests, docs)
3. Review smaller PRs
4. Use on-demand reviews instead of automatic

## Cost and Rate Limits

### Google AI Studio (Free Tier)

- **Requests**: 60 requests per minute
- **Tokens**: Generous free quota for Gemini models
- **Cost**: Free for personal use and testing

### Google Cloud (Production)

- **Pricing**: Pay-per-use based on input/output tokens
- **Rate limits**: Configurable based on quota
- **Cost estimation**: Check [Google Cloud Pricing](https://cloud.google.com/vertex-ai/pricing)

For most repositories, the free tier is sufficient for PR reviews.

## Security Best Practices

1. **Never commit API keys** to the repository
2. **Use repository secrets** for all credentials
3. **Rotate API keys** periodically (every 90 days recommended)
4. **Monitor usage** in Google Cloud Console
5. **Set up billing alerts** to prevent unexpected charges
6. **Use Workload Identity Federation** for production (more secure than API keys)

## Advanced Configuration

### Using Workload Identity Federation

For enhanced security in enterprise environments:

1. Set up a Google Cloud project
2. Configure Workload Identity Federation
3. Follow the guide at: [Gemini CLI WIF Setup](https://medium.com/google-cloud/goodbye-api-keys-gemini-cli-github-actions-with-workload-identity-federation-6d4fae9e936b)

### Custom Review Prompts

To customize what Gemini looks for, you can add a `PROMPT` parameter to the action (requires modifying the action or using the official `run-gemini-cli` action).

### Integration with Existing CI/CD

The PR review workflow runs independently of other CI workflows. You can:

1. Make review a required check for merging
2. Run it in parallel with build/test jobs
3. Use review results to block merges (requires additional configuration)

## Resources

- [Google AI Studio](https://aistudio.google.com/apikey) - Get API keys
- [Gemini Models Documentation](https://ai.google.dev/models) - Available models
- [GitHub Actions Documentation](https://docs.github.com/en/actions) - Workflow syntax
- [Gemini AI Code Reviewer Action](https://github.com/marketplace/actions/gemini-ai-code-reviewer) - Action details

## Support

For issues with:
- **This workflow**: Open an issue in this repository
- **The GitHub Action**: Visit [truongnh1992/gemini-ai-code-reviewer](https://github.com/truongnh1992/gemini-ai-code-reviewer)
- **Gemini API**: Check [Google AI documentation](https://ai.google.dev/docs)

---

**Last Updated**: November 2025
**Gemini Model**: 3.0 Pro
**Action Version**: v9.1.0
