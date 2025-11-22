# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- GitHub Copilot instructions file (`.github/copilot-instructions.md`)
- `global.json` to pin .NET SDK version to 9.0.100
- Architecture Decision Record: ADR-0001 for initial server architecture
- Proper `src/` directory structure for projects
- Formal namespacing convention: `Ancplua.Mcp.<ServerName>`

### Changed
- Reorganized repository structure: moved projects to `src/` directory
- Renamed projects to follow formal naming convention:
  - `WorkstationServer` → `Ancplua.Mcp.WorkstationServer`
  - `HttpServer` → `Ancplua.Mcp.HttpServer`
  - `WorkstationServer.Tests` → `Ancplua.Mcp.WorkstationServer.Tests`
  - `HttpServer.Tests` → `Ancplua.Mcp.HttpServer.Tests`
- Updated all namespaces to match new project names
- Updated solution file to reflect new project structure and folder organization
- Updated README.md to reflect new structure and provide comprehensive guidance
- Updated local validation script to reference new `src/` directory

### Removed
- Removed `CLAUDE.md` (replaced with `.github/copilot-instructions.md`)

## [0.1.0] - 2025-11-22

### Added
- Initial release of ancplua-mcp
- WorkstationServer (stdio-based MCP server)
- HttpServer (ASP.NET Core-based MCP server)
- Core tool implementations
- Basic documentation and examples
