# ADR-0108: Replace Dependabot with Renovate

## Status

Proposed

## Date

2025-11-25

## Context

We currently use GitHub Dependabot for automated dependency updates. While functional, it has limitations that create friction:

### Current Pain Points

1. **No auto-merge capability** — Every patch bump requires manual "Merge" click
2. **Poor grouping** — 8 GitHub Actions updates = 8 separate PRs (see PR #37)
3. **No SDK tracking** — Cannot update `global.json` SDK version
4. **Limited scheduling** — Only daily/weekly, no rate limiting
5. **Duplicate effort** — Manual review of obviously-safe updates

### Why Not Both?

Running Dependabot + Renovate simultaneously would create:
- Duplicate PRs for the same updates
- Confusion about which bot "owns" what
- Merge conflicts between competing PRs
- No clear source of truth

**Verdict:** Running both is worse than either alone.

## Decision

**Replace Dependabot entirely with Renovate.**

### Rationale

| Capability | Dependabot | Renovate | Winner |
|------------|------------|----------|--------|
| NuGet packages | ✅ | ✅ | Tie |
| GitHub Actions | ✅ | ✅ | Tie |
| .NET SDK (global.json) | ❌ | ✅ | Renovate |
| Auto-merge patches | ❌ (needs workflow) | ✅ (built-in) | Renovate |
| Grouping (monorepo) | Basic | Advanced | Renovate |
| Rate limiting | ❌ | ✅ | Renovate |
| Schedule flexibility | Basic | Cron + windows | Renovate |
| Dashboard PR | ❌ | ✅ | Renovate |
| Regex managers | ❌ | ✅ | Renovate |

### What We Gain

1. **Auto-merge for patches** — `coverlet 6.0.2 → 6.0.4` merges automatically if CI passes
2. **Grouped PRs** — All GitHub Actions in one PR, all test dependencies in another
3. **SDK updates** — `global.json` tracked and updated
4. **Dashboard** — Single PR showing all pending updates
5. **Less noise** — Fewer PRs, less clicking

### What We Lose

1. **Native GitHub integration** — Dependabot is built-in, Renovate is a GitHub App
2. **Zero config** — Dependabot works out of box, Renovate needs `renovate.json`
3. **Security alerts integration** — Dependabot security PRs are tighter integrated

### Migration Path

1. Create `renovate.json` with equivalent + enhanced config
2. Verify Renovate creates expected PRs
3. Delete `.github/dependabot.yml`
4. Close any open Dependabot PRs

## Consequences

### Positive

- Reduced manual effort (auto-merge)
- Fewer, better-organized PRs
- SDK version tracking
- Single source of truth for dependency automation

### Negative

- One-time migration effort
- Learning curve for Renovate config syntax
- External GitHub App dependency (vs native Dependabot)

### Risks

| Risk | Mitigation |
|------|------------|
| Renovate App goes down | Fallback: re-enable Dependabot temporarily |
| Config complexity | Start simple, iterate |
| Auto-merge breaks things | Require CI pass + use conservative rules |

## Alternatives Considered

### 1. Keep Dependabot, add auto-merge workflow

- Requires custom workflow for auto-merge
- Still no SDK tracking
- Still poor grouping
- **Rejected:** More maintenance for less capability

### 2. Run both with careful scoping

- Dependabot for security alerts only
- Renovate for version updates
- **Rejected:** Complexity outweighs benefit; Renovate handles security too

### 3. Status quo

- Keep Dependabot as-is
- **Rejected:** Ongoing friction with manual merges and noisy PRs

## References

- [Renovate Documentation](https://docs.renovatebot.com/)
- [Renovate vs Dependabot comparison](https://docs.renovatebot.com/dependabot-comparison/)
- PR #26: Example of Dependabot single-package PR
- PR #37: Example of 8-update grouped PR (still 8 commits)
