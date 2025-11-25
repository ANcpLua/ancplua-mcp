# ADR-0109: C# Modernization Wave 1 (C#14 GA Transition)

## Status

Proposed

## Date

2025-11-25

## Context

We are running .NET 10 (SDK 10.0.100) with C# 14 features enabled via `LangVersion preview`. Now that C# 14 is GA, we should:

1. Transition from `preview` to explicit `14.0`
2. Adopt additional C#12/13/14 features where they reduce boilerplate
3. Establish patterns for the codebase going forward

### Current Feature Usage

| Feature | C# Version | Status | Example |
|---------|------------|--------|---------|
| `field` keyword | C#14 | ✅ Used | `AIServiceInfo.Name` |
| `required` modifier | C#11+ | ✅ Used | `AIServiceInfo`, `ArchitectureViolation` |
| File-scoped namespaces | C#10 | ✅ Used | All files |
| Nullable reference types | C#8+ | ✅ Used | Enabled globally |
| Record types | C#9+ | ✅ Used | `ProcessResult`, `WhisperMetadata` |
| Collection expressions | C#12 | ❌ Not used | `["status", "--porcelain"]` vs `new[] {...}` |
| Primary constructors | C#12 | ❌ Not used | Record positional params instead |
| Extension members | C#14 | ❌ Not used | N/A for current patterns |

### What's Working Well

- `field` keyword in init accessors removes backing field boilerplate
- `required` enforces compile-time initialization
- Records provide value equality and immutability
- Strict analyzers catch issues early

### What Could Be Better

1. **LangVersion `preview`** — Risk: Preview features may change. Lock to `14.0`.
2. **Array.Empty<T>()** — Verbose. Collection expressions `[]` are shorter and clearer.
3. **Array initializers** — `new[] { "a", "b" }` → `["a", "b"]`
4. **List initializers** — `new List<string> { "a" }` → `["a"]` (with target typing)

## Decision

**Execute Modernization Wave 1 with three phases:**

### Phase 1: LangVersion Lock (Breaking Change Risk: None)

Change `Directory.Build.props`:
```xml
<LangVersion>14.0</LangVersion>
```

### Phase 2: Collection Expressions (Breaking Change Risk: None)

Replace throughout codebase:
```csharp
// Before
Array.Empty<string>()
new[] { "status", "--porcelain" }
new List<string>(files.Count + 2) { "add", "--" }

// After
[]
["status", "--porcelain"]
["add", "--", .. files]  // spread operator
```

### Phase 3: Additional `required` on DTOs (Breaking Change Risk: None for internal types)

Add `required` to DTO properties that must be initialized:
```csharp
// WhisperMetadata - make essential fields required
public required string? Project { get; init; }  // Was optional, keep optional
// No changes - WhisperMetadata is intentionally all-optional
```

**NOT adopting in Wave 1:**
- Primary constructors — Records with positional params already serve this purpose
- Extension members — No compelling use case yet
- Null-conditional assignment — Few applicable locations

## Consequences

### Positive

- LangVersion locked to stable, no surprise breaks from preview changes
- Collection expressions reduce visual noise (~30+ instances)
- Consistent modern idioms across codebase
- Better code review: reviewers expect `[]` syntax

### Negative

- One-time churn in diffs
- Developers must know collection expression syntax
- Some older analyzers/tools may not recognize `[]`

### Risks

| Risk | Mitigation |
|------|------------|
| CI breaks on LangVersion change | Test locally first, SDK is already 10.0.100 |
| Collection expression edge cases | Keep array type explicit where inference fails |
| Spread operator perf | Only use in non-hot paths, benchmarks if needed |

## Alternatives Considered

### 1. Stay on preview

- **Rejected:** Preview features can change, pinning to GA is safer

### 2. Adopt all C#14 features aggressively

- **Rejected:** Extension members and other features don't have compelling use cases yet. Adopt when needed.

### 3. Skip collection expressions

- **Rejected:** They're idiomatic C#12+, reduce noise, and we're already on C#14

## References

- [C# 14 What's New](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [Collection Expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions)
- [Field Keyword](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/field)
- `Directory.Build.props` — Current LangVersion configuration
