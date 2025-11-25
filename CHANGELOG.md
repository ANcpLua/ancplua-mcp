# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Documentation

- **tool-contracts.md**: Expanded comprehensive MCP tool contracts documentation covering all servers:
  - GitHubAppsServer (Gemini, Jules, CodeRabbit, Codecov, AI Orchestration tools)
  - AIServicesServer (ServiceDiscovery tools)
  - RoslynMetricsServer (NuGet, Architecture, Roslyn Metrics tools)
  - WorkstationServer/HttpServer (Git, FileSystem, CI tools)
  - DebugTools (shared introspection tools)
- **ADR Numbering Fix**: Resolved duplicate ADR-001 and ADR-002 IDs:
  - `adr-001-instruction-based-tools.md` → `adr-004-instruction-based-tools.md`
  - `adr-002-docker-registry-submission.md` → `adr-005-docker-registry-submission.md`
- **Spec Numbering Fix**: Resolved duplicate spec-002 ID:
  - `spec-002-csharp-14-features.md` → `spec-003-csharp-14-features.md`

### New Features
- **DebugTools**: Added `Ancplua.Mcp.DebugTools` shared library for MCP server introspection.
  - `debug_print_env`: Environment variables with sensitive value masking.
  - `debug_get_server_info`: Server metadata, version, transport, runtime info.
  - `debug_get_http_headers`: HTTP request headers (HTTP transport only).
  - `debug_get_user_claims`: Authentication claims (HTTP transport only).
  - `debug_get_all`: Combined debug information in a single call.
  - See [ADR-003](docs/decisions/adr-003-debug-mcp-tools.md) and [spec-005](docs/specs/spec-005-debug-mcp-tools.md).

### Architecture & Infrastructure
- **Upgrade**: Migrated entire solution to **.NET 10** (C# 14).
- **ServiceDefaults**: Introduced `Ancplua.Mcp.ServiceDefaults` for centralized OpenTelemetry, health checks, and resilience.
- **Refactor**: Standardized server startup patterns and strict stdio logging discipline.
- **Structure**: Reorganized repository into `src/` with "Insight Spine" architecture.

### Code Ergonomics (C# 14)
- **`field` Keyword**: Adopted C# 14 `field` keyword in `AIServiceInfo` and `WhisperMessage` for cleaner property definitions.
- **Extensions**: Utilized C# 14 `extension` types for cleaner `IHostApplicationBuilder` extensions (e.g., `IsDevelopment` property).
- **Performance**: Prepared codebase for .NET 10 JIT Loop Inversion and stack-allocated delegates.

### Documentation
- **Consolidation**: Merged peripheral guides into core specs (`spec-002`, `spec-github-apps`) and `README.md`.
- **New Guides**: Added documentation for GitHub Copilot Coding Agent and JetBrains Rider integration.
- **Agent Workflow**: Updated `CLAUDE.md` and `Requirements*.md` for "Ultimate Agent" workflow.

### Security
- **Removed**: Unsafe Jules auto-merge workflows.

## [0.1.0] - 2025-11-22
### Added
- Initial release of `ancplua-mcp` family.
- **Servers**: WorkstationServer (stdio), HttpServer (HTTP).
- **Tools**: FileSystem, Git, CI/CD, NuGet, Roslyn support.