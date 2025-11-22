# ADR-001: Use .NET 10 as Target Framework

## Architecture Decision Record

### Title
Use .NET 10 as the Target Framework for ancplua-mcp

### Status
Accepted

### Date
2025-11-22

## Context

The ancplua-mcp project needs to select a .NET framework version for implementation. Key factors influencing this decision:

- The project is starting fresh with no legacy code to migrate
- Modern C# language features improve code quality and developer productivity
- Performance improvements in newer .NET versions benefit server applications
- Long-term support and security updates are critical for production use
- The target audience (developers) typically have recent tooling installed
- MCP protocol implementations benefit from modern async/await patterns and HTTP features

## Decision

We will use **.NET 10** as the target framework for all projects in the ancplua-mcp solution.

All C# projects will target `net10.0`:
```xml
<TargetFramework>net10.0</TargetFramework>
```

## Rationale

**Why .NET 10:**

1. **Latest Features**: .NET 10 provides the latest C# 14 language features and runtime improvements
2. **Performance**: Significant performance improvements in ASP.NET Core 10 and runtime
3. **Modern APIs**: Access to latest APIs for HTTP, JSON serialization, and async programming
4. **Support Timeline**: .NET 10 is a Long Term Support (LTS) release with 3 years of support
5. **ASP.NET Core Improvements**: Enhanced minimal APIs, better OpenAPI support, and improved performance for HttpServer
6. **Tooling**: Excellent IDE support in Visual Studio 2022, VS Code, and JetBrains Rider

**Trade-offs Accepted:**

- Requires users to have .NET 10 SDK installed
- Cutting edge release may have initial adoption challenges
- Some enterprise environments may not have adopted .NET 10 yet

## Consequences

### Positive Consequences
- Access to latest C# 14 features (improved pattern matching, collection expressions, and more)
- Better performance for both WorkstationServer and HttpServer
- Improved developer experience with modern language features
- Smaller runtime footprint with recent optimizations
- Better tooling support and diagnostics
- LTS support provides 3 years of updates and security patches

### Negative Consequences
- Users must install .NET 10 SDK to build and run the servers
- Cutting edge technology may have initial adoption hurdles
- Some corporate environments may be slower to adopt

### Neutral Consequences
- Regular SDK updates will be required to stay current
- Documentation must clearly state .NET 10 requirement
- CI/CD pipelines must use .NET 10 SDK

## Alternatives Considered

### Alternative 1: .NET 8 (LTS)
Use .NET 8 Long-Term Support release

**Pros:**
- 3 years of support (until November 2026)
- More conservative choice for enterprise adoption
- Slightly wider user base currently installed
- Production-proven stability

**Cons:**
- Missing newest C# 13 and .NET 9 features
- Slightly lower performance than .NET 9
- Less modern codebase

**Decision:** Not chosen because the project is new and can benefit from latest features. The LTS support of .NET 10 provides excellent long-term stability while giving us access to the most modern .NET platform.

### Alternative 2: .NET 6 (LTS)
Use older .NET 6 LTS release for maximum compatibility

**Pros:**
- Widest compatibility
- Longest remaining support (until November 2024 ended, now November 2027 for some)
- Most conservative choice

**Cons:**
- Significantly older features
- Lower performance
- Missing many modern APIs
- Outdated for new projects started in 2025

**Decision:** Not chosen as it's too conservative for a new project. Using 2+ year old technology doesn't align with project goals.

### Alternative 3: Multi-targeting (net8.0;net10.0)
Support both .NET 8 and .NET 10

**Pros:**
- Wider compatibility
- Users can choose their version

**Cons:**
- Increased complexity
- More testing required
- Can't use .NET 10-specific features
- Maintenance burden

**Decision:** Not chosen due to increased complexity without significant benefit for our use case.

## Implementation Notes

### Migration Path
If future .NET versions are released:
1. Evaluate new .NET version features
2. Create migration branch
3. Update target framework to new version
4. Test thoroughly
5. Update documentation and examples
6. Release new major version

### Required Changes
- All .csproj files specify `<TargetFramework>net10.0</TargetFramework>`
- GitHub Actions workflow uses `dotnet-version: '10.0.x'`
- README clearly states .NET 10 requirement
- Installation instructions include .NET 10 SDK download link

### Timeline
- Immediate: All projects use .NET 10
- 2027-11: .NET 10 LTS support ends, evaluate migration to .NET 12 LTS

## Related Decisions

- ADR-002: Use ASP.NET Core for HttpServer (benefits from .NET 10 improvements)
- ADR-003: Use xUnit for testing (fully compatible with .NET 10)
- Spec-001: MCP Protocol Implementation (uses modern async/await patterns)

## References

- [.NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [.NET Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [C# 14 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [ASP.NET Core 10 Release Notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)

## Notes

This decision should be revisited when .NET 12 LTS is released and when .NET 10 approaches end of support.

For contributors: If .NET 10 is not installed, download from https://dotnet.microsoft.com/download/dotnet/10.0

---

**Template Version**: 1.0  
**Last Updated**: 2025-11-22
