# Spec-007: Modern Configuration Standards (2025)

## 1. Build Configuration (`Directory.Build.props`)

The global build properties must enforce the following:

### 1.1 Compiler & Language
- **Target Framework**: `net10.0` (already in projects, but enforced implicitly).
- **Language Version**: `preview` or `14.0` to unlock the `field` keyword and semi-auto properties.
- **Nullable Reference Types**: `enable` (Global).
- **Implicit Usings**: `enable`.

### 1.2 Static Analysis
- **Analysis Level**: `latest-all` (Includes security, performance, usage, style).
- **Treat Warnings As Errors**: `true`.
- **Enforce Code Style**: `true` (Build fails on formatting issues).

### 1.3 Packaging & Restore
- **Lock Files**: `true` (Deterministic restore).
- **Central Package Management**: Enabled (via `Directory.Packages.props`).

### 1.4 Artifacts
- **UseArtifactsOutput**: `true` (New .NET layout standard).

## 2. Code Coverage (`codecov.yml`)

### 2.1 Coverage Goals
- **Project**: 80% target (Ambitious but achievable for a tool library).
- **Patch**: 100% target (New code must be fully tested).
- **Threshold**: 1% (Allow minor fluctuations).

### 2.2 Flags & Components
- **Unit Tests**: Flag `unittests` for standard test suites.
- **Integration**: Flag `integration` (future use).

### 2.3 Exclusion
- Generated code (`*.g.cs`, `*.Designer.cs`).
- Test projects themselves.
- Documentation and Tooling scripts.

## 3. Dependencies
- **Dependabot**: Sole authority for updates.
- **Renovate**: Disabled/Removed.
