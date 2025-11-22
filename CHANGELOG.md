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
- Documentation (README, CLAUDE integration guide, ARCHITECTURE)
- Example configuration files for Claude Desktop and JetBrains Rider
- Local validation script
- GitHub Actions CI workflow
- Dependabot configuration
- GitHub Copilot instructions file (.github/copilot-instructions.md)
- global.json for .NET SDK version pinning (9.0)

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

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- N/A

## [0.1.0] - 2025-11-22

### Added
- Initial release of ancplua-mcp
- WorkstationServer (stdio-based MCP server)
- HttpServer (ASP.NET Core-based MCP server)
- Core tool implementations
- Basic documentation and examples
