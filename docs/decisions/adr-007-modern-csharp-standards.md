# ADR-007: Adoption of Modern .NET 10 and C# 14 Standards

## Status
Accepted

## Context
As of November 2025, .NET 10 and C# 14 represent the current state-of-the-art in the Microsoft ecosystem. To ensure `ancplua-mcp` remains a high-quality reference implementation, we must adopt the latest compiler features, safety guarantees, and tooling configurations.

Current configuration is minimal and does not leverage:
- C# 14 features (e.g., `field` keyword, enhanced pattern matching).
- .NET 10 performance defaults (TieredPGO, DATAS).
- Strict static analysis (warnings as errors).
- Modern coverage policies.

## Decision
We will upgrade the global build configuration and code coverage policies to the "2025 Gold Standard".

### Key Changes
1.  **C# 14 & .NET 10**: Enforce `<LangVersion>14</LangVersion>` and target `net10.0`.
2.  **Strictness**: Enable `<TreatWarningsAsErrors>` and `<AnalysisLevel>latest-all</AnalysisLevel>`.
3.  **Performance**: Enable `<TieredPGO>` explicitly (though default in .NET 10, being explicit documents intent).
4.  **Coverage**: Adopt a strict `codecov.yml` with "patch" coverage enforcement to prevent regression.
5.  **Tooling**: Consolidation of dependency management to Dependabot (removing Renovate).

## Consequences
### Positive
- Codebase will use the most expressive and performant features available.
- Bugs will be caught at build time via strict analysis.
- "Left-shifting" of quality checks.

### Negative
- Initial build might fail due to new warnings (will need immediate fixing).
- Higher barrier to entry for contributors not on latest toolsets (VS 2025 / .NET 10 SDK required).

## Compliance
- All new code must compile without warnings.
- CI must pass strict coverage checks.
