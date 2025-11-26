# Spec-0108: Renovate Configuration

## Overview

Implementation specification for replacing Dependabot with Renovate per ADR-0108.

## Scope

| In Scope | Out of Scope |
|----------|--------------|
| NuGet package updates | Build system changes |
| GitHub Actions updates | NUKE adoption (separate effort) |
| .NET SDK updates (global.json) | Other tooling |
| Auto-merge configuration | Security scanning (SAST) |
| Grouping strategy | |

## Deliverables

### 1. renovate.json

Create `/renovate.json` at repo root:

```json
{
  "$schema": "https://docs.renovatebot.com/renovate-schema.json",
  "extends": [
    "config:recommended",
    ":semanticCommits",
    ":automergeMinor",
    "group:dotnetCore"
  ],
  "timezone": "Europe/Vienna",
  "schedule": ["after 6am and before 9am on Monday"],
  "labels": ["dependencies", "automated"],
  "prHourlyLimit": 3,
  "prConcurrentLimit": 5,
  "packageRules": [
    {
      "description": "Auto-merge patch updates for test dependencies",
      "matchPackagePatterns": [
        "^coverlet",
        "^xunit",
        "^Microsoft\\.NET\\.Test",
        "^FluentAssertions",
        "^Moq",
        "^NSubstitute"
      ],
      "matchUpdateTypes": ["patch"],
      "automerge": true,
      "automergeType": "pr",
      "platformAutomerge": true
    },
    {
      "description": "Auto-merge patch updates for analyzers",
      "matchPackagePatterns": [
        "^StyleCop",
        "^SonarAnalyzer",
        "^Roslynator",
        "^Microsoft\\.CodeAnalysis"
      ],
      "matchUpdateTypes": ["patch"],
      "automerge": true
    },
    {
      "description": "Group all GitHub Actions updates",
      "matchManagers": ["github-actions"],
      "groupName": "GitHub Actions",
      "automerge": true,
      "automergeType": "pr",
      "schedule": ["after 6am on Monday"]
    },
    {
      "description": "Group Microsoft.Extensions.* packages",
      "matchPackagePatterns": ["^Microsoft\\.Extensions\\."],
      "groupName": "Microsoft.Extensions"
    },
    {
      "description": "Group OpenTelemetry packages",
      "matchPackagePatterns": ["^OpenTelemetry"],
      "groupName": "OpenTelemetry"
    },
    {
      "description": "Group MCP SDK packages",
      "matchPackagePatterns": ["^ModelContextProtocol"],
      "groupName": "MCP SDK"
    },
    {
      "description": ".NET SDK updates - manual review",
      "matchManagers": ["nuget"],
      "matchPackageNames": ["dotnet-sdk"],
      "automerge": false,
      "labels": ["dependencies", "sdk", "manual-review"]
    },
    {
      "description": "Major updates require manual review",
      "matchUpdateTypes": ["major"],
      "automerge": false,
      "labels": ["dependencies", "breaking-change"]
    }
  ],
  "dotnet": {
    "enabled": true
  },
  "vulnerabilityAlerts": {
    "enabled": true,
    "labels": ["security", "dependencies"]
  }
}
```

### 2. Delete Dependabot Config

Remove `/.github/dependabot.yml` after Renovate is verified working.

### 3. GitHub App Installation

1. Install Renovate GitHub App: https://github.com/apps/renovate
2. Grant access to `ancplua-mcp` repository
3. Renovate will create initial "Dependency Dashboard" issue

## Configuration Explained

### Grouping Strategy

| Group | Packages | Why |
|-------|----------|-----|
| GitHub Actions | All actions/* | Reduce noise, same risk profile |
| Microsoft.Extensions | DI, Logging, Config | Often updated together |
| OpenTelemetry | All OTEL packages | Version alignment required |
| MCP SDK | ModelContextProtocol.* | Core dependency, review together |

### Auto-merge Rules

| Package Type | Patch | Minor | Major |
|--------------|-------|-------|-------|
| Test deps (xunit, coverlet) | ✅ Auto | ✅ Auto | ❌ Manual |
| Analyzers (StyleCop, etc) | ✅ Auto | ❌ Manual | ❌ Manual |
| GitHub Actions | ✅ Auto | ✅ Auto | ❌ Manual |
| .NET SDK | ❌ Manual | ❌ Manual | ❌ Manual |
| Everything else | ❌ Manual | ❌ Manual | ❌ Manual |

### Schedule

- **When:** Monday 6-9am Vienna time
- **Why:** Start of week, before work, CI has capacity
- **Rate limit:** Max 3 PRs/hour, 5 concurrent

## Migration Checklist

- [ ] Create `renovate.json` per spec
- [ ] Install Renovate GitHub App
- [ ] Wait for Dependency Dashboard issue
- [ ] Verify first PR created correctly
- [ ] Verify auto-merge works for patch update
- [ ] Close all open Dependabot PRs
- [ ] Delete `.github/dependabot.yml`
- [ ] Update CHANGELOG.md
- [ ] Update CLAUDE.md if needed

## Verification

### Success Criteria

1. Renovate creates grouped PRs (not 8 separate ones)
2. Patch updates auto-merge after CI passes
3. Major updates require manual approval
4. Dependency Dashboard issue tracks all updates
5. No duplicate PRs from Dependabot

### Rollback Plan

If Renovate fails:
1. Delete `renovate.json`
2. Restore `.github/dependabot.yml` from git history
3. Uninstall Renovate GitHub App

## References

- ADR-0108: Decision to replace Dependabot
- [Renovate Presets](https://docs.renovatebot.com/presets-config/)
- [Renovate Package Rules](https://docs.renovatebot.com/configuration-options/#packagerules)
- [Platform Automerge](https://docs.renovatebot.com/configuration-options/#platformautomerge)
