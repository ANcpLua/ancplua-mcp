# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure with Ancplua.Mcp.WorkstationServer and Ancplua.Mcp.HttpServer
- FileSystemTools for file and directory operations
- GitTools for git repository operations
- CiTools for CI/CD and build operations
- Official MCP SDK integration
  - ModelContextProtocol 0.4.0-preview.3 package for WorkstationServer
  - ModelContextProtocol.AspNetCore 0.3.0-preview.4 package for HttpServer
  - MCP attributes ([McpServerToolType], [McpServerTool], [Description]) on all tools
- Test projects for both servers
- Ancplua.Mcp.Testing shared test helpers project
  - McpTestHost for in-process MCP server testing
  - Test extension methods and utilities
- Documentation (README, CLAUDE integration guide, ARCHITECTURE)
- Example configuration files for Claude Desktop and JetBrains Rider
- Local validation script
- GitHub Actions CI workflow
- Dependabot configuration
- GitHub Copilot instructions file (.github/copilot-instructions.md)
- global.json for .NET SDK version pinning (10.0)
- .editorconfig for consistent C# code style
- Directory.Packages.props for centralized package version management
- Spec-002: Comprehensive C# 14 Features Reference documentation
  - Extension members (instance and static)
  - Field keyword for simplified properties
  - Implicit span conversions
  - Unbound generic types with nameof
  - Simple lambda parameters with modifiers
  - Partial constructors and events
  - User-defined compound assignment operators
  - Null-conditional assignment
  - Best practices and usage examples for ancplua-mcp
- GitHub Actions workflow for automated PR reviews with Jules AI
  - Automatic review on PR open/update/reopen
  - On-demand review via `/jules-review` comment
  - Customizable review prompts for code quality, security, and performance
  - Comprehensive setup guide in docs/jules-pr-review-setup.md

### Changed
- Reorganized project structure to follow target architecture
  - Moved servers from root to src/ directory
  - Renamed WorkstationServer to Ancplua.Mcp.WorkstationServer
  - Renamed HttpServer to Ancplua.Mcp.HttpServer
  - Renamed test projects to match new server names
- Updated all project references and solution file to reflect new paths
- Updated README.md with new project structure and paths
- Updated local-validate.sh to use new src/ directory structure
- Implemented official MCP SDK for both servers
  - WorkstationServer now uses stdio transport with Host.CreateApplicationBuilder
  - HttpServer now uses HTTP transport with MapMcp() endpoint
  - All tool classes updated to use proper MCP attributes and namespaces
  - Tool discovery now automatic via WithToolsFromAssembly()
- Comprehensive documentation overhaul for MCP architecture
  - Rewrote docs/ARCHITECTURE.md with detailed server architecture, tool design, and testing strategy
  - Updated README.md with comprehensive architectural documentation and conventions
  - Standardized docs/examples/*.mcp.json files with consistent format and relative paths
  - Enhanced tooling/scripts/local-validate.sh with shellcheck and markdownlint support
  - Improved .github/workflows/ci.yml with CodeQL, dependency review, and TruffleHog security scanning
- Updated target framework from .NET 9 to .NET 10 LTS
  - ADR-001: Updated to reference .NET 10, C# 14, and ASP.NET Core 10
  - global.json: Updated SDK version to 10.0.0
  - README.md: Updated prerequisites to require .NET 10.0 SDK or later
  - All .csproj files: Updated TargetFramework from net9.0 to net10.0
  - GitHub Actions CI workflow: Updated dotnet-version from 9.0.x to 10.0.x
  - Documentation references updated to .NET 10, C# 14, and ASP.NET Core 10
  - Migration timeline updated to align with .NET 10 LTS support cycle (3 years)

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- Removed dangerous auto-merge workflows and excessive cleanup schedules to prevent potential infinite loops and unauthorized code merges.
- Ensured `BeksOmega/jules-action` references are valid.

## [0.1.0] - 2025-11-22

### Added
- Initial release of ancplua-mcp
- WorkstationServer (stdio-based MCP server)
- HttpServer (ASP.NET Core-based MCP server)
- Core tool implementations
- Basic documentation and examples
