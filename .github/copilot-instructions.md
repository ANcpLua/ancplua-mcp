# GitHub Copilot Instructions for ancplua-mcp

> **Repository Role:** C# Model Context Protocol (MCP) servers for development workflows and tools.

This repository provides MCP servers for AI-assisted development. It does not contain GitHub Copilot plugins; those live in separate repositories and consume these servers via `.mcp.json` configuration files.

---

## Repository Architecture

### Target Structure

This repository follows a standard .NET solution structure:

```text
ancplua-mcp/
├── README.md
├── CHANGELOG.md
├── .gitignore
├── global.json                              # Pins .NET SDK version
│
├── src/
│   ├── Ancplua.Mcp.WorkstationServer/      # Stdio MCP server for local dev tools
│   │   ├── Ancplua.Mcp.WorkstationServer.csproj
│   │   ├── Program.cs
│   │   └── Tools/                          # [McpServerToolType] classes
│   │
│   └── Ancplua.Mcp.HttpServer/            # ASP.NET Core MCP server
│       ├── Ancplua.Mcp.HttpServer.csproj
│       ├── Program.cs
│       └── Tools/
│
├── tests/
│   ├── Ancplua.Mcp.WorkstationServer.Tests/
│   └── Ancplua.Mcp.HttpServer.Tests/
│
├── docs/
│   ├── specs/                              # Feature specifications
│   ├── decisions/                          # Architecture Decision Records (ADRs)
│   └── examples/                           # Configuration examples
│
├── tooling/
│   └── scripts/
│       └── local-validate.sh              # Local build and test script
│
└── .github/
    └── workflows/
        ├── ci.yml
        └── dependabot.yml
```

---

## Development Guidelines

### Server Types

This repository contains two MCP server implementations:

1. **WorkstationServer**: Stdio-based console app for direct integration with Claude Desktop and other MCP clients
2. **HttpServer**: ASP.NET Core-based HTTP server for web-based integrations

Both servers expose the same tools but use different transport mechanisms.

### MCP SDK Usage

Servers use the official C# MCP SDK packages:

- **`ModelContextProtocol`**: Core hosting, DI, `AddMcpServer()`, `WithStdioServerTransport()`, `WithToolsFromAssembly()`
- **`ModelContextProtocol.AspNetCore`**: For HTTP servers (`WithHttpTransport`, `MapMcp`)
- **`ModelContextProtocol.Core`**: Only for shared libraries or custom clients (use sparingly)

**Guidelines:**
- WorkstationServer should use `ModelContextProtocol` only
- HttpServer should use `ModelContextProtocol` + `ModelContextProtocol.AspNetCore`
- Only add `ModelContextProtocol.Core` if building shared libraries

### Tool Implementation Patterns

Tools should follow C# SDK conventions:

```csharp
[McpServerToolType]
public static class FileSystemTools
{
    [McpServerTool]
    [Description("Read contents of a file")]
    public static async Task<string> ReadFileAsync(
        [Description("Path to the file")] string path)
    {
        // Implementation
    }
}
```

**Rules:**
- Use `[McpServerToolType]` on static classes that group related tools
- Use `[McpServerTool]` on individual tool methods
- Use `[Description]` attributes for tools and parameters
- Keep tool names stable and descriptive
- Input schemas must be well-defined
- Return values should be predictable (clear text or structured JSON)

### Testing Requirements

- All non-trivial tools must have tests
- Use xUnit for test projects
- Follow the existing test patterns in the repository
- Tests should be independent and repeatable

### Build and Validation Workflow

Before committing changes:

1. **Restore dependencies**: `dotnet restore`
2. **Build**: `dotnet build`
3. **Run tests**: `dotnet test`
4. **Run local validation script**: `./tooling/scripts/local-validate.sh`

The CI pipeline mirrors these steps and must pass for all pull requests.

---

## Documentation Requirements

For any change that affects external behavior (new tools, changed contracts, new servers):

### 1. CHANGELOG.md
- Add entries under `[Unreleased]` section
- Use categories: Added, Changed, Fixed, Deprecated, Removed, Security
- Include server and tool names affected

### 2. Specifications (`docs/specs/`)
- Create or update specs for new/modified features
- Use `spec-template.md` as a starting point
- Include tool signatures, inputs/outputs, and usage patterns

### 3. Architecture Decision Records (`docs/decisions/`)
- Document architectural changes (new server types, major design choices)
- Use `adr-template.md` as a starting point
- Include status, decision drivers, alternatives considered, and consequences

### 4. README.md
- Update when adding servers or changing tool categories
- Keep usage instructions current

---

## Code Quality Standards

### Principles
- **Predictable and modular**: Keep server layout consistent
- **Stable contracts**: Tool signatures should be versioned and stable
- **Well-tested**: Non-trivial functionality requires tests
- **Focused changes**: Keep related changes grouped and minimal

### C# Coding Standards
- Target modern .NET (net8.0 or net9.0)
- Enable nullable reference types
- Use implicit usings
- Follow .NET naming conventions
- Use XML documentation comments for public APIs

### Security
- Never commit secrets
- Use documented configuration mechanisms only (`appsettings.*.json`, environment variables)
- Validate all inputs in tools
- Follow principle of least privilege

---

## Common Tasks

### Adding a New MCP Tool

1. Choose the appropriate Tools class (FileSystemTools, GitTools, CiTools, or create new)
2. Add a new static method with `[McpServerTool]` attribute
3. Add `[Description]` attributes for the method and all parameters
4. Implement the tool functionality
5. Add tests in the corresponding test project
6. Update relevant documentation (spec, CHANGELOG)

### Adding a New Server

1. Create new project under `src/`
2. Add appropriate MCP SDK package references
3. Implement Program.cs with MCP hosting
4. Create corresponding test project under `tests/`
5. Update solution file
6. Add documentation (spec, ADR if architectural)
7. Update README.md

### Modifying Tool Signatures

1. Consider versioning implications
2. Update tool implementation
3. Update tests
4. Document changes in CHANGELOG under "Changed"
5. Update relevant specs

---

## CI/CD

The repository uses GitHub Actions for continuous integration:

- **Build and Test**: Runs on all pushes and pull requests
- **Code Quality**: Runs `dotnet format` checks
- **Security Scan**: Uses Trivy for vulnerability scanning

Dependabot automatically checks for:
- NuGet package updates (weekly)
- GitHub Actions updates (weekly)

---

## Integration with Other Repositories

This repository is independent and provides MCP servers as standalone processes:

- Other repositories consume these servers via `.mcp.json` configuration
- Document server names, startup commands, and exposed tools clearly
- Avoid tight coupling to any specific client repository
- Changes to tool contracts should be backward compatible when possible

---

## Best Practices

### When Making Changes

1. **Understand the context**: Read existing code, specs, and ADRs
2. **Plan before implementing**: Outline changes for complex features
3. **Make focused commits**: One logical change per commit
4. **Test thoroughly**: Run full validation suite
5. **Update documentation**: Keep CHANGELOG, specs, and ADRs current
6. **Review before pushing**: Ensure all checks pass

### When Adding Dependencies

1. Check if the dependency is truly necessary
2. Verify it's compatible with the target .NET version
3. Review security advisories
4. Add to appropriate project only (don't add to all projects)
5. Document rationale if adding major dependencies

### When Refactoring

1. Ensure tests exist before refactoring
2. Run tests frequently during refactoring
3. Keep changes minimal and focused
4. Don't mix refactoring with feature changes
5. Update documentation if external behavior changes

---

## Getting Help

- **MCP Protocol Documentation**: https://modelcontextprotocol.io/
- **C# MCP SDK**: https://github.com/modelcontextprotocol/csharp-sdk
- **Repository Issues**: Check existing issues and discussions

---

This file guides how GitHub Copilot should assist in this repository. When uncertain about patterns or conventions, refer to this document and existing code examples.
