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
2. **Performance**: Significant performance improvements in ASP.NET Core and runtime
3. **Modern APIs**: Access to latest APIs for HTTP, JSON serialization, and async programming
4. **Support Timeline**: .NET 10 is a Long-Term Support (LTS) release with 3 years of support
5. **ASP.NET Core Improvements**: Enhanced minimal APIs, better OpenAPI support, and improved performance for HttpServer
6. **Tooling**: Excellent IDE support in Visual Studio 2026, VS Code, and JetBrains Rider

**Trade-offs Accepted:**

- Requires users to have .NET 10 SDK installed
- Cutting-edge release may have initial adoption challenges
- Some enterprise environments may not have adopted .NET 10 yet

## Consequences

### Positive Consequences
- Access to latest C# 14 features (extension members, field keyword, implicit span conversions, null-conditional assignment)
- Better performance for both WorkstationServer and HttpServer
- Improved developer experience with modern language features
- Smaller runtime footprint with recent optimizations
- Better tooling support and diagnostics
- Long-term support (3 years) provides stability

### Negative Consequences
- Users must install .NET 10 SDK to build and run the servers
- As an LTS release, may not have the very latest experimental features
- May require updates when .NET 12 LTS is released
- Some corporate environments may be slower to adopt

### Neutral Consequences
- Regular SDK updates will be required to stay current
- Documentation must clearly state .NET 10 requirement
- CI/CD pipelines must use .NET 10 SDK

## Alternatives Considered

### Alternative 1: .NET 9 (STS)
Use .NET 9 Standard-Term Support release

**Pros:**
- Still very modern with C# 13 features
- 18 months of support
- Production-proven stability
- Good performance improvements

**Cons:**
- Missing newest C# 14 features
- Shorter support window than LTS releases
- Slightly lower performance than .NET 10
- Will require migration sooner

**Decision:** Not chosen because .NET 10 LTS provides both the latest features AND long-term support, making it a better choice for a new project.

### Alternative 2: .NET 8 (LTS)
Use older .NET 8 LTS release for maximum compatibility

**Pros:**
- Widest compatibility with existing enterprise environments
- Extended support until November 2026
- Most conservative choice
- Well-proven in production

**Cons:**
- Missing C# 13 and C# 14 features
- Lower performance compared to .NET 10
- Missing modern APIs and runtime improvements
- Outdated for new projects started in 2025

**Decision:** Not chosen as it's too conservative for a new project starting in 2025. Using previous-generation LTS doesn't align with project goals of leveraging latest language features.

### Alternative 3: Multi-targeting (net9.0;net10.0)
Support both .NET 9 and .NET 10

**Pros:**
- Wider compatibility
- Users can choose their version
- Gradual migration path

**Cons:**
- Increased complexity
- More testing required
- Can't use .NET 10-specific features (C# 14)
- Maintenance burden
- Complicates CI/CD pipelines

**Decision:** Not chosen due to increased complexity without significant benefit for our use case. Single-target approach simplifies development and testing.

## Implementation Notes

### Migration Path
If .NET 10 support ends before project maturity:
1. Evaluate .NET 12 LTS features
2. Create migration branch
3. Update target framework to .NET 12
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
- 2027-11: Mid-cycle review and evaluation
- 2028-11: End of .NET 10 support, must migrate to .NET 12 LTS

## Related Decisions

- ADR-002: Use ASP.NET Core for HttpServer (benefits from .NET 10 improvements)
- ADR-003: Use xUnit for testing (fully compatible with .NET 10)
- Spec-001: MCP Protocol Implementation (uses modern async/await patterns)
- Spec-002: C# 14 Features Reference (documents C# 14 language features)

## References

- [.NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [.NET Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [C# 14 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [ASP.NET Core 10 Release Notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)

## Notes

This decision should be revisited in November 2027 as .NET 10 approaches mid-support cycle and when .NET 12 LTS planning begins.

For contributors: If .NET 10 is not installed, download from https://dotnet.microsoft.com/download/dotnet/10.0

---

**Template Version**: 1.0  
**Last Updated**: 2025-11-22
