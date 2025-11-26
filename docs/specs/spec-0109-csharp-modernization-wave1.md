# Spec-0109: C# Modernization Wave 1 Implementation

## Overview

Implementation specification for ADR-0109: Transition to C#14 GA and adopt collection expressions consistently.

## Scope

| In Scope | Out of Scope |
|----------|--------------|
| LangVersion `preview` → `14.0` | Primary constructors |
| Collection expressions adoption | Extension members |
| Consistent `[]` syntax | New `required` additions |

## Current State Analysis

### Already Using Collection Expressions ✅
```csharp
// GitTools.cs - Already modernized!
["status", "--porcelain"]
["log", "--oneline", $"--max-count={maxCount}"]
["commit", "-m", message]

// ServiceDiscoveryTools.cs - Partially modernized
Capabilities = ["code-review", "generation", "refactoring", "analysis"]

// CiTools.cs - Already modernized
["build", target]
["test", target]
```

### Needs Modernization ❌

#### 1. Directory.Build.props
```xml
<!-- Before -->
<LangVersion>preview</LangVersion>

<!-- After -->
<LangVersion>14.0</LangVersion>
```

#### 2. Array.Empty<T>() → []

| File | Line | Before | After |
|------|------|--------|-------|
| `WhisperMessage.cs` | 154 | `Array.Empty<string>()` | `[]` |
| `AIServiceInfo.cs` | 52 | `Array.Empty<string>()` | `[]` |

#### 3. new[] {...} → [...]

| File | Lines | Count |
|------|-------|-------|
| `ServiceDiscoveryTools.cs` | 27, 112, 125, 133, 139, 152, 164, 170, 175, 181, 186, 191, 196 | 13 instances |

#### 4. new List<T> → Collection Expression

| File | Line | Before | After |
|------|------|--------|-------|
| `CommandParser.cs` | 42 | `new List<string>()` | `List<string> parts = []` |
| `GitTools.cs` | 165 | `new List<string>(capacity) { ... }` | `List<string> args = ["add", "--", .. files]` |
| `WhisperMessage.cs` | 92 | `new List<string>()` | `List<string> errors = []` |

## Detailed Changes

### Phase 1: LangVersion Lock

**File:** `Directory.Build.props`

```xml
<LangVersion>14.0</LangVersion>
```

**Verification:**
```bash
dotnet build
# Should succeed with no warnings about preview features
```

### Phase 2: Collection Expressions

#### WhisperMessage.cs (Line 154)
```csharp
// Before
public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

// After
public IReadOnlyList<string> Errors { get; init; } = [];
```

#### AIServiceInfo.cs (Line 52)
```csharp
// Before
public required IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();

// After
public required IReadOnlyList<string> Capabilities { get; init; } = [];
```

#### CommandParser.cs (Line 42)
```csharp
// Before
var parts = new List<string>();

// After
List<string> parts = [];
```

#### GitTools.cs (Lines 165-166)
```csharp
// Before
var args = new List<string>(files.Count + 2) { "add", "--" };
args.AddRange(files);

// After
List<string> args = ["add", "--", .. files];
```

#### WhisperMessage.cs (Line 92)
```csharp
// Before
var errors = new List<string>();

// After
List<string> errors = [];
```

#### ServiceDiscoveryTools.cs (Multiple)
```csharp
// Before (Line 27)
var services = new[]
{
    new AiServiceInfo { ... }
};

// After
AiServiceInfo[] services =
[
    new AiServiceInfo { ... }
];

// Before (Line 112, 125, etc.)
capabilities = new[] { "a", "b", "c" }

// After
capabilities = ["a", "b", "c"]

// Before (Line 133)
features = new[] { "multi-file-editing", "git-integration" }

// After
features = ["multi-file-editing", "git-integration"]
```

## Implementation Checklist

### Pre-flight
- [ ] Ensure SDK 10.0.100 is installed (`dotnet --version`)
- [ ] Clean build passes (`dotnet build`)
- [ ] All tests pass (`dotnet test`)

### Phase 1: LangVersion
- [ ] Update `Directory.Build.props`: `preview` → `14.0`
- [ ] Build and verify no warnings

### Phase 2: Collection Expressions
- [ ] `WhisperMessage.cs:154` — `Array.Empty<string>()` → `[]`
- [ ] `AIServiceInfo.cs:52` — `Array.Empty<string>()` → `[]`
- [ ] `CommandParser.cs:42` — `new List<string>()` → `[]`
- [ ] `GitTools.cs:165-166` — Use spread operator `[..]`
- [ ] `WhisperMessage.cs:92` — `new List<string>()` → `[]`
- [ ] `ServiceDiscoveryTools.cs` — All 13 `new[]` instances

### Verification
- [ ] `dotnet build --no-restore`
- [ ] `dotnet test --no-build`
- [ ] `dotnet format --verify-no-changes`

### Documentation
- [ ] Update CHANGELOG.md under `[Unreleased]`

## CHANGELOG Entry

```markdown
### Changed
- Locked LangVersion to 14.0 (from preview) for C#14 GA stability
- Modernized collection initializers to use C#12 collection expressions throughout codebase
```

## Rollback Plan

If issues arise:
1. Revert `Directory.Build.props` to `LangVersion preview`
2. Collection expressions are backward compatible with `preview` — no rollback needed

## References

- ADR-0109: C# Modernization Wave 1
- [Collection Expressions Specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions)
- [Spread Operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions#spread-element)
