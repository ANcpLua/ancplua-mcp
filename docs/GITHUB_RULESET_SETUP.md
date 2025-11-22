# GitHub Ruleset Configuration Guide

This guide explains how to configure GitHub rulesets for automated code reviews and branch protection.

## Overview

Our repository uses GitHub rulesets to enforce:
- Required PR approvals
- Status checks (CI/CD)
- Code quality and security scanning
- Automated Copilot reviews

## Current Workflows

We have the following GitHub Actions workflows that should be required:

### CI Workflow (`.github/workflows/ci.yml`)

Status checks to require:
- `build-and-test` - Builds and tests the .NET code
- `code-quality` - Checks code formatting
- `codeql` - Security analysis
- `dependency-review` - Reviews dependencies for vulnerabilities (PRs only)
- `secret-scan` - Scans for leaked secrets
- `security-scan` - Trivy vulnerability scanner

### PR Review Workflow (`.github/workflows/pr-review.yml`)

Status checks to require:
- `auto-review` - Jules AI automated PR review

## Ruleset Configuration Steps

### 1. Navigate to Ruleset Settings

1. Go to your repository on GitHub
2. Click **Settings**
3. In the left sidebar, expand **Rules** under "Code and automation"
4. Click **Rulesets**
5. Click on your existing ruleset: **"Copilot review for default branch"**

### 2. Configure Required Status Checks

Scroll down to the **"Require status checks to pass"** section:

#### Enable the Setting

1. ✅ Check **"Require status checks to pass"**
2. ✅ Check **"Require branches to be up to date before merging"**
3. ✅ Check **"Do not require status checks on creation"**

#### Add Required Checks

Click **"Add checks"** button and add these checks one by one:

**From CI Workflow:**
```
build-and-test
code-quality
codeql / Analyze (csharp)
dependency-review
secret-scan
security-scan
```

**From PR Review Workflow:**
```
auto-review
```

#### Screenshot Reference

Your configuration should look like:

```
☑ Require status checks to pass
  ☑ Require branches to be up to date before merging
  ☑ Do not require status checks on creation

  Required checks:
  ✓ build-and-test
  ✓ code-quality
  ✓ codeql / Analyze (csharp)
  ✓ dependency-review
  ✓ secret-scan
  ✓ security-scan
  ✓ auto-review
```

### 3. Configure Code Scanning

Scroll to **"Require code scanning results"**:

1. ✅ Check **"Require code scanning results"**
2. Click **"Add tool"**
3. Select **CodeQL**
4. Set **Security alerts** to: **High or higher** ✓
5. Set **Alerts** to: **Errors** ✓

### 4. Configure Code Quality

Scroll to **"Require code quality results"**:

1. ✅ Check **"Require code quality results"**
2. Set **Severity** to: **Errors** ✓

### 5. Enable Copilot Features

#### Automatic Copilot Reviews

1. ✅ Check **"Automatically request Copilot code review"**

#### Manage Static Analysis Tools

1. ✅ Check **"Manage static analysis tools in Copilot code review"**
2. Click **"Select tools"**
3. Enable:
   - ✅ **CodeQL** - Standard queries
   - ✅ **ESLint** - Standard rules
   - ✅ **PMD** - Standard rules

### 6. Additional Branch Rules (Recommended)

For maximum protection, also enable these in the **Branch rules** section:

#### Highly Recommended

- ✅ **Require linear history** - Prevents messy merge commits
- ✅ **Require a pull request before merging**
  - Required approvals: **2**
  - ✅ Dismiss stale pull request approvals when new commits are pushed
  - ✅ Require conversation resolution before merging

#### Optional (Enable if Needed)

- **Require signed commits** - See [SIGNED_COMMITS.md](./SIGNED_COMMITS.md)
  - ⚠️ Only enable after team members have GPG/SSH signing configured
- **Require review from Code Owners** - Uses `.github/CODEOWNERS`
  - Only needed if you want specific owners to review specific files

### 7. Save Changes

1. Scroll to the bottom
2. Click **"Save changes"**

## Verifying Configuration

### Test with a New PR

1. Create a test branch:
   ```bash
   git checkout -b test-ruleset-config
   ```

2. Make a small change:
   ```bash
   echo "# Test" >> README.md
   git add README.md
   git commit -m "Test: Verify ruleset configuration"
   git push -u origin test-ruleset-config
   ```

3. Create a PR on GitHub

4. Verify that:
   - ✅ All required checks appear in the PR
   - ✅ Copilot automatically adds a review
   - ✅ PR cannot be merged until checks pass
   - ✅ PR requires 2 approvals

### Check Status Checks

In your PR, you should see a section like:

```
All checks have passed
  ✓ build-and-test
  ✓ code-quality
  ✓ codeql / Analyze (csharp)
  ✓ secret-scan
  ✓ security-scan
  ✓ auto-review

This pull request requires 2 approving reviews
```

## Troubleshooting

### Checks Not Appearing

**Issue:** Required checks don't show up in PRs

**Solutions:**
1. Check that workflow files exist in `.github/workflows/`
2. Ensure workflows are on the `main` branch
3. Verify workflow triggers include `pull_request`
4. Check that workflow jobs have the correct names

### Checks Failing

**Issue:** Checks are red/failing

**Solutions:**

1. **build-and-test failing:**
   ```bash
   # Run locally to debug
   dotnet restore
   dotnet build
   dotnet test
   ```

2. **code-quality failing:**
   ```bash
   # Check formatting
   dotnet format --verify-no-changes

   # Auto-fix formatting
   dotnet format
   git add .
   git commit -m "Fix: Apply code formatting"
   ```

3. **CodeQL failing:**
   - Check the CodeQL results in Security tab
   - Fix the security issues identified

4. **secret-scan failing:**
   - Review the TruffleHog output
   - Remove any secrets from code
   - Use environment variables or GitHub Secrets instead

### Can't Merge PR

**Issue:** "Merging is blocked" even with passing checks

**Possible Causes:**
1. Not enough approvals (need 2)
2. Conversations not resolved
3. Branch not up to date with base branch
4. One or more checks still pending

**Solutions:**
1. Request reviews from team members
2. Resolve all PR conversations
3. Update branch:
   ```bash
   git checkout your-branch
   git pull origin main
   git push
   ```

## GitHub CLI Commands

Useful commands for managing PRs:

```bash
# View PR checks
gh pr checks

# View PR status
gh pr view

# Request reviews
gh pr review --approve

# Merge PR (if all checks pass)
gh pr merge --merge
```

## Ruleset Export/Import

### Export Current Ruleset

You can export the ruleset configuration for backup or sharing:

1. Go to Settings → Rules → Rulesets
2. Click the "..." menu on your ruleset
3. Select "Export as JSON"
4. Save the file

### Import Ruleset

To import a ruleset configuration:

1. Go to Settings → Rules → Rulesets
2. Click "Import a ruleset"
3. Upload your JSON file

## Recommended Ruleset Configuration (JSON)

Here's the recommended ruleset configuration for this repository:

```json
{
  "name": "Copilot review for default branch",
  "target": "branch",
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/main"],
      "exclude": []
    }
  },
  "rules": [
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 2,
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": true,
        "require_last_push_approval": false,
        "required_review_thread_resolution": true
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "required_status_checks": [
          {
            "context": "build-and-test"
          },
          {
            "context": "code-quality"
          },
          {
            "context": "codeql / Analyze (csharp)"
          },
          {
            "context": "secret-scan"
          },
          {
            "context": "security-scan"
          },
          {
            "context": "auto-review"
          }
        ]
      }
    },
    {
      "type": "code_scanning",
      "parameters": {
        "code_scanning_tools": [
          {
            "tool": "CodeQL",
            "security_alerts_threshold": "high_or_higher",
            "alerts_threshold": "errors"
          }
        ]
      }
    }
  ]
}
```

## Integration with MCP Servers

If you're using this repository's MCP servers with GitHub Copilot:

1. Ensure your MCP configuration in GitHub Copilot settings includes these servers
2. The servers can provide additional context during code reviews
3. See [MCP configuration docs](../README.md#mcp-configuration) for details

## Additional Resources

- [GitHub: Managing rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets)
- [GitHub: Required status checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks)
- [GitHub: Code scanning](https://docs.github.com/en/code-security/code-scanning/automatically-scanning-your-code-for-vulnerabilities-and-errors/about-code-scanning)
- [GitHub: Copilot code review](https://docs.github.com/en/copilot/using-github-copilot/code-review/using-copilot-code-review)

## Support

If you encounter issues:
1. Check the [Troubleshooting](#troubleshooting) section above
2. Review GitHub Actions workflow logs
3. Check GitHub Security tab for scanning results
4. Contact repository administrators
