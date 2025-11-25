# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **spec-0107-whispermesh-phase1-implementation.md**: Comprehensive specification for WhisperMesh Phase 1 Dolphin Pod integration
  - Defines `IWhisperMeshClient` interface for pub/sub operations
  - Specifies `NatsWhisperMeshClient` implementation using NATS JetStream
  - Documents `WhisperAggregator` service for deduplication and tier-based sorting
  - Defines `AggregateDiscoveries` MCP tool contract (request/response DTOs)
  - Includes configuration schema, NATS subject naming conventions, and integration test requirements
  - Authorizes Phase 1 implementation per CLAUDE.md mandatory workflow

### Fixed
- **NuGet Package Resolution**: Fixed all .NET 10 build warnings
  - Removed global `FrameworkReference` for `Microsoft.AspNetCore.App` from `Directory.Packages.props` (NETSDK1086)
  - Removed prunable `Microsoft.Extensions.Hosting` PackageReferences from servers (NU1510)
  - Removed prunable `Microsoft.Extensions.Logging` PackageReferences where in-box (NU1510)
  - Removed prunable `Microsoft.Extensions.Diagnostics.HealthChecks` from ServiceDefaults (NU1510)
  - Removed prunable `Microsoft.Extensions.Configuration.Json` from AIServicesServer (NU1510)
- **ModelContextProtocol RC2 Dependency Pinning**: Added explicit PackageVersion entries to pin
  transitive dependencies from MCP 0.4.0-preview.3 (which depends on 10.0.0-rc.2 abstractions)
  to GA 10.0.0 versions via `CentralPackageTransitivePinningEnabled`
- **NuGet.config Package Source Mapping**: Updated to allow Microsoft.Extensions.* packages
  from both `dotnet10` feed and `nuget.org` for proper transitive dependency resolution

### Changed
- **Directory.Packages.props**: Reorganized Microsoft Extensions packages
  - Removed in-box packages that don't need explicit references
  - Added explicit pins for abstractions packages to override MCP's RC2 dependencies
  - Added NATS.Client packages for WhisperMesh central version management
  - Added OpenTelemetry.Api for WhisperMesh
- **DebugTools.csproj**: Added explicit `FrameworkReference` for `Microsoft.AspNetCore.App`
  (required for `IHttpContextAccessor` in non-web SDK projects)
- **Testing.csproj**: Retained `Microsoft.Extensions.Hosting` PackageReference
  (required for `Host.CreateApplicationBuilder()` in test utilities)

### Added
- **Dual-AI Code Review**: Automatic parallel review by Claude and Jules on every PR
  - `jules-auto-review.yml`: Jules reviews PRs and creates fix PRs if issues found
  - `claude-code-review.yml`: Claude posts review comments
  - Both run automatically without human intervention
  - Documented in CLAUDE.md Section 10
- **Ancplua.Mcp.CoreTools**: New shared library consolidating tools from HttpServer and WorkstationServer
  - Implements ADR-006 (Core Tools Consolidation) and spec-006
  - Eliminates code duplication between servers
- **Ancplua.Mcp.CoreTools.Tests**: New test project with 47 tests
  - ProcessRunner deadlock prevention tests (large stdout/stderr)
  - CommandParser quote handling tests
  - FileSystemTools path traversal security tests
- **Path Traversal Protection**: FileSystemTools now validates all paths against `AllowedBasePath`
  - Configurable via `FILESYSTEM_TOOLS_BASE_PATH` environment variable
  - Prevents access to files outside allowed directory

### Fixed
- **ProcessRunner Deadlock**: Fixed critical deadlock in HttpServer process execution
  - Now reads stdout/stderr asynchronously before awaiting process exit
  - Implements proper cancellation with `process.Kill(entireProcessTree: true)`
- **CommandParser**: Fixed quote handling
  - Now supports escaped quotes (`\"`) within quoted strings
  - Supports single quotes (`'...'`)
  - Detects and reports unclosed quotes
- **Error Message Truncation**: ProcessRunner now truncates error output to prevent sensitive data leakage

### Changed
- **GitTools.AddAsync**: Now accepts `IReadOnlyList<string>` instead of single string
  - Properly handles filenames with spaces
  - Uses `--` separator to prevent argument injection
- **IsPackable**: CoreTools.csproj and DebugTools.csproj now have `IsPackable=false` to prevent accidental NuGet publishing
- **Security Warnings**: Added explicit security warnings to `RunCommandAsync` documentation
- **JulesTools Refactoring**: Migrated from instructional to programmatic Jules API integration
  - `CreateJulesSession`: Creates Jules sessions via `jules.googleapis.com/v1alpha` API
  - `GetJulesInfo`: Returns configuration status and Jules capabilities
  - Removed static class - now uses DI for `IConfiguration` access
  - Added `JULES_API_KEY` environment variable support
- **ServiceDiscoveryTools**: Fixed Jules API endpoint from incorrect URL to `jules.googleapis.com/v1alpha`
- **AIOrchestrationTools**: Updated Jules descriptions to clarify it creates PRs, not review comments
- **MCP Config Examples**: Added required `type` and `tools` fields per GitHub Copilot spec

### Removed
- **jules-auto-reviewer.yml**: Removed workflow that referenced non-existent `beksomega/jules-action@v1`
- **jules-cleanup.yml**: Removed workflow with same broken action reference
- **pr-review.yml**: Disabled orphaned workflow in GitHub Actions

### Security
- **Path Traversal Prevention**: All FileSystemTools operations now validate paths
- **Command Injection Documentation**: Added security warnings to RunCommandAsync
- **Jules Workflows Removed**: Removed unsafe auto-merge workflows per security decision (PR #22)

### Documentation

- **jules-chatops.yml**: Added example ChatOps workflow for Jules integration
  - Triggers via `/jules [task]` comments on PRs
  - Creates Jules sessions via API with plan approval required
  - Posts session links back to PR for monitoring
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