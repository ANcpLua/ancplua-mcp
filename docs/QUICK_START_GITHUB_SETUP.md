# Quick Start: GitHub Repository Setup

This guide provides a quick overview of setting up your GitHub repository with automated reviews, branch protection, and code quality checks.

## What's Included

This repository now has:

1. ✅ **CI/CD Workflows** - Automated builds, tests, and security scanning
2. ✅ **Code Owners** - Automatic review assignment
3. ✅ **Signed Commits Guide** - Setup instructions for commit signing
4. ✅ **Ruleset Configuration Guide** - GitHub branch protection setup

## Quick Setup Checklist

### 1. Review the Documentation

- [ ] Read [GITHUB_RULESET_SETUP.md](./GITHUB_RULESET_SETUP.md) - GitHub ruleset configuration
- [ ] Read [SIGNED_COMMITS.md](./SIGNED_COMMITS.md) - Commit signing setup
- [ ] Review [CODEOWNERS](../.github/CODEOWNERS) - Code ownership rules

### 2. Configure Your GitHub Repository

#### A. Add Required Status Checks

Follow [GITHUB_RULESET_SETUP.md](./GITHUB_RULESET_SETUP.md#2-configure-required-status-checks) to add:

```
Required Status Checks:
☐ build-and-test
☐ code-quality
☐ codeql / Analyze (csharp)
☐ dependency-review
☐ secret-scan
☐ security-scan
☐ auto-review
```

#### B. Configure Branch Protection

In your GitHub ruleset for the `main` branch, enable:

```
Branch Rules:
☐ Require linear history
☐ Require a pull request before merging
  ☐ Required approvals: 2
  ☐ Dismiss stale approvals when new commits pushed
  ☐ Require conversation resolution before merging
☐ Require status checks to pass
  ☐ Require branches to be up to date before merging
☐ Require code scanning results (CodeQL)
☐ Require code quality results (Errors)
☐ Automatically request Copilot code review
```

#### C. Enable Copilot Static Analysis

In your ruleset, under "Manage static analysis tools":

```
Static Analysis Tools:
☐ CodeQL (Standard queries)
☐ ESLint (Standard rules)
☐ PMD (Standard rules)
```

### 3. Set Up Your Local Environment

#### A. Configure Signed Commits (Recommended)

Choose one:

**Option 1: GPG Signing** (More traditional)
```bash
gpg --full-generate-key
gpg --list-secret-keys --keyid-format=long
git config --global user.signingkey YOUR_KEY_ID
git config --global commit.gpgsign true
```

**Option 2: SSH Signing** (Simpler)
```bash
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true
```

See [SIGNED_COMMITS.md](./SIGNED_COMMITS.md) for detailed instructions.

#### B. Test Your Setup

```bash
# Create test commit
git commit --allow-empty -m "Test: Verify signed commit setup"

# Verify signature
git verify-commit HEAD

# Push to test branch
git checkout -b test-setup
git push -u origin test-setup
```

### 4. Commit These New Files

```bash
# Add new files
git add .github/CODEOWNERS
git add docs/GITHUB_RULESET_SETUP.md
git add docs/SIGNED_COMMITS.md
git add docs/QUICK_START_GITHUB_SETUP.md

# Commit
git commit -m "docs: Add GitHub setup documentation and CODEOWNERS

- Add CODEOWNERS file for automatic review assignment
- Add GitHub ruleset configuration guide
- Add signed commits setup guide
- Add quick start guide for repository setup

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"

# Push to new branch
git push -u origin docs/github-setup
```

### 5. Create Pull Request

```bash
# Create PR using GitHub CLI
gh pr create \
  --title "docs: Add GitHub setup documentation and CODEOWNERS" \
  --body "$(cat <<'EOF'
## Summary
- Add CODEOWNERS file for automatic review assignment
- Add comprehensive GitHub ruleset configuration guide
- Add signed commits setup guide with GPG and SSH options
- Add quick start guide for repository setup

## What's New

### Files Added
- `.github/CODEOWNERS` - Defines code ownership for automatic reviews
- `docs/GITHUB_RULESET_SETUP.md` - Complete guide for configuring GitHub rulesets
- `docs/SIGNED_COMMITS.md` - Step-by-step guide for commit signing
- `docs/QUICK_START_GITHUB_SETUP.md` - Quick reference for setup

### Benefits
1. Automatic assignment of reviewers based on changed files
2. Clear documentation for required status checks
3. Commit signing for enhanced security
4. Streamlined onboarding for new contributors

## Test Plan
- [x] CODEOWNERS syntax validated
- [x] Documentation reviewed for accuracy
- [ ] Test PR creation to verify CODEOWNERS works
- [ ] Verify ruleset configuration with test PR

## Next Steps
After merging:
1. Configure GitHub ruleset following GITHUB_RULESET_SETUP.md
2. Team members set up signed commits using SIGNED_COMMITS.md
3. Test with a new PR to verify all protections work

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

## What These Files Do

### `.github/CODEOWNERS`
- Automatically assigns `@ANcpLua` as reviewer for all PRs
- Can be extended with specific owners for different paths
- Integrates with branch protection rules

### `docs/GITHUB_RULESET_SETUP.md`
- Complete guide for configuring GitHub repository rulesets
- Lists all required status checks from your CI/CD workflows
- Step-by-step instructions with screenshots
- Troubleshooting section for common issues

### `docs/SIGNED_COMMITS.md`
- Instructions for both GPG and SSH signing
- Covers macOS, Linux, and Windows
- Troubleshooting common GPG issues
- Explains why signing is important

### `docs/QUICK_START_GITHUB_SETUP.md` (This File)
- Quick reference checklist
- Links to detailed docs
- Minimal steps to get started

## Existing Workflows

Your repository already has excellent CI/CD workflows:

### `.github/workflows/ci.yml`
Includes:
- Build and test (.NET)
- Code quality checks (dotnet format)
- CodeQL security analysis
- Dependency review
- Secret scanning (TruffleHog)
- Security scanning (Trivy)

### `.github/workflows/pr-review.yml`
Includes:
- Automatic PR review with Jules AI
- On-demand reviews via `/jules-review` comment

## GitHub Copilot MCP Configuration

To use your MCP servers with GitHub Copilot, add this to GitHub's Copilot settings:

```json
{
  "mcpServers": {
    "ancplua-workstation": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/full/path/to/ancplua-mcp/src/Ancplua.Mcp.WorkstationServer/Ancplua.Mcp.WorkstationServer.csproj"
      ]
    }
  }
}
```

Replace `/full/path/to/ancplua-mcp` with your actual repository path.

## Verification Steps

After setup, verify everything works:

1. **Create a test PR**
   ```bash
   git checkout -b test-branch
   echo "# Test" >> README.md
   git add README.md
   git commit -m "Test: Verify setup"
   git push -u origin test-branch
   gh pr create --fill
   ```

2. **Check that PR shows:**
   - ✅ All required status checks
   - ✅ Copilot automatic review
   - ✅ Code owner assigned as reviewer
   - ✅ Requires 2 approvals
   - ✅ Can't merge until checks pass

3. **Test signed commits:**
   ```bash
   git log --show-signature -1
   ```
   Should show "Good signature" or similar

## Support

- [GitHub Rulesets Documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets)
- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [GPG Signing Documentation](https://docs.github.com/en/authentication/managing-commit-signature-verification)

## Summary

You now have:
- ✅ Automated CI/CD with comprehensive checks
- ✅ Code ownership rules
- ✅ Documentation for GitHub setup
- ✅ Documentation for signed commits
- ✅ Ready-to-use ruleset configuration

**Next:** Follow the steps in [GITHUB_RULESET_SETUP.md](./GITHUB_RULESET_SETUP.md) to configure your GitHub repository!
