# Spec-002: C# 14 Features Reference

## Specification

### Title
C# 14 Language Features Reference for ancplua-mcp

### Status
Active

### Date
2025-11-22

## Overview

This document provides a comprehensive reference for C# 14 language features that can be leveraged in the ancplua-mcp codebase. C# 14 is supported on .NET 10 and includes several significant language enhancements that improve code expressiveness, safety, and maintainability.

## C# 14 Features

### 1. Extension Members

C# 14 introduces new syntax to define extension members beyond just methods. This enables extension properties, static extension members, and user-defined operators.

#### Syntax

```csharp
public static class Enumerable
{
    // Extension block for instance members
    extension<TSource>(IEnumerable<TSource> source)
    {
        // Extension property
        public bool IsEmpty => !source.Any();

        // Extension method
        public IEnumerable<TSource> Where(Func<TSource, bool> predicate) { ... }
    }

    // Extension block for static members
    extension<TSource>(IEnumerable<TSource>)
    {
        // Static extension method
        public static IEnumerable<TSource> Combine(
            IEnumerable<TSource> first,
            IEnumerable<TSource> second) { ... }

        // Static extension property
        public static IEnumerable<TSource> Identity => Enumerable.Empty<TSource>();

        // User-defined operator
        public static IEnumerable<TSource> operator + (
            IEnumerable<TSource> left,
            IEnumerable<TSource> right) => left.Concat(right);
    }
}
```

#### Usage in ancplua-mcp

Extension members can be used to enhance MCP tool types without modifying core classes:

```csharp
// Potential use in FileSystemTools
extension(FileInfo file)
{
    public bool IsHidden => file.Attributes.HasFlag(FileAttributes.Hidden);
    public string RelativePath(string basePath) => Path.GetRelativePath(basePath, file.FullName);
}
```

### 2. The `field` Keyword

The `field` keyword enables property accessor bodies without declaring explicit backing fields, with the compiler synthesizing the backing field automatically.

#### Syntax

```csharp
// Before C# 14 - explicit backing field required
private string _msg;
public string Message
{
    get => _msg;
    set => _msg = value ?? throw new ArgumentNullException(nameof(value));
}

// C# 14 - simplified with field keyword
public string Message
{
    get;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}
```

#### Usage in ancplua-mcp

This simplifies property validation in tool implementations:

```csharp
public class GitToolsOptions
{
    public string RepositoryPath
    {
        get;
        set => field = Path.GetFullPath(value ?? throw new ArgumentNullException(nameof(value)));
    }
}
```

### 3. Implicit Span Conversions

C# 14 provides first-class support for `System.Span<T>` and `System.ReadOnlySpan<T>` with new implicit conversions, allowing more natural programming with these performance-oriented types.

#### Key Improvements

- Span types can be extension method receivers
- Implicit conversions between `ReadOnlySpan<T>`, `Span<T>`, and `T[]`
- Better generic type inference for span-related operations

#### Usage in ancplua-mcp

Improves performance-critical file and string operations:

```csharp
// More natural span usage for file content processing
public void ProcessFileContent(ReadOnlySpan<char> content)
{
    // Implicit conversions make this more ergonomic
    var lines = content.SplitLines();
    foreach (var line in lines)
    {
        // Process each line efficiently
    }
}
```

### 4. Unbound Generic Types and `nameof`

The `nameof` operator can now accept unbound generic types, returning the type name without generic parameters.

#### Syntax

```csharp
// C# 14 - unbound generic types work
var name = nameof(List<>);  // Returns "List"

// Previously required closed generic types
var oldWay = nameof(List<int>);  // Returns "List"
```

#### Usage in ancplua-mcp

Useful for logging and diagnostic messages:

```csharp
public class McpTool<T>
{
    private readonly ILogger _logger;

    public McpTool(ILogger logger)
    {
        _logger = logger;
        _logger.LogInformation($"Initializing {nameof(McpTool<>)} for type {typeof(T).Name}");
    }
}
```

### 5. Simple Lambda Parameters with Modifiers

Parameter modifiers (`scoped`, `ref`, `in`, `out`, `ref readonly`) can be added to lambda parameters without specifying parameter types.

#### Syntax

```csharp
// C# 14 - modifiers without types
TryParse<int> parse1 = (text, out result) => Int32.TryParse(text, out result);

// Previously required types for modifiers
TryParse<int> parse2 = (string text, out int result) => Int32.TryParse(text, out result);
```

#### Usage in ancplua-mcp

Simplifies functional programming patterns in tool implementations:

```csharp
// Simplified delegate for parsing operations
Func<string, (bool success, int value)> parseWithModifier =
    (input) => TryParse(input, out var result) ? (true, result) : (false, 0);
```

### 6. More Partial Members

Instance constructors and events can now be declared as partial members.

#### Partial Constructors

```csharp
// Declaration
public partial class McpServer
{
    partial void InitializeServer();
}

// Implementation
public partial class McpServer
{
    partial void InitializeServer()
    {
        // Implementation details
    }
}
```

#### Partial Events

```csharp
// Defining declaration (field-like event)
public partial class McpServer
{
    partial event EventHandler? ServerStarted;
}

// Implementing declaration (with add/remove)
public partial class McpServer
{
    partial event EventHandler? ServerStarted
    {
        add { /* custom logic */ }
        remove { /* custom logic */ }
    }
}
```

#### Usage in ancplua-mcp

Useful for separating generated code from hand-written code in MCP server implementations.

### 7. User-Defined Compound Assignment Operators

Types can now define custom compound assignment operators that are invoked for `+=`, `-=`, and other compound assignments.

#### Syntax

```csharp
public class Counter
{
    public int Value { get; set; }

    public static Counter operator +(Counter c, int value)
    {
        return new Counter { Value = c.Value + value };
    }
}

// Usage with compound assignment
var counter = new Counter { Value = 10 };
counter += 5;  // Invokes operator +
```

#### Usage in ancplua-mcp

Can be used for builder patterns or accumulator types in tool results:

```csharp
public class GitDiffResult
{
    public List<string> Changes { get; } = new();

    public static GitDiffResult operator +(GitDiffResult result, string change)
    {
        result.Changes.Add(change);
        return result;
    }
}
```

### 8. Null-Conditional Assignment

The null-conditional member access operators (`?.` and `?[]`) can now be used on the left-hand side of assignments and compound assignments.

#### Syntax

```csharp
// Before C# 14 - explicit null check required
if (customer is not null)
{
    customer.Order = GetCurrentOrder();
}

// C# 14 - simplified with null-conditional assignment
customer?.Order = GetCurrentOrder();

// Compound assignment also supported
customer?.OrderCount += 1;
```

#### Important Behavior

- Right-hand side is evaluated only when left-hand side is not null
- Increment/decrement (`++`, `--`) are NOT supported

#### Usage in ancplua-mcp

Simplifies optional parameter handling in MCP tool implementations:

```csharp
public class GitCommitOptions
{
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
}

public void ApplyOptions(GitCommitOptions? options, GitConfig config)
{
    // Simplified null-safe assignment
    options?.AuthorName = config.DefaultAuthor;
    options?.AuthorEmail = config.DefaultEmail;
}
```

## Best Practices for ancplua-mcp

### 1. Extension Members

**DO:**
- Use extension members to add tool-specific functionality to common types
- Group related extensions in appropriately named static classes
- Document extension members clearly with XML comments

**DON'T:**
- Overuse extension members where inheritance or composition is more appropriate
- Create extension members that conflict with existing instance members

### 2. Field Keyword

**DO:**
- Use `field` keyword for properties with simple validation logic
- Prefer `field` over explicit backing fields when accessor logic is minimal

**DON'T:**
- Use `field` when complex multi-statement validation is needed
- Confuse `field` with actual field names in types that have both

### 3. Span Types

**DO:**
- Use span types for performance-critical file and string operations
- Leverage implicit conversions to simplify span-based APIs
- Profile before and after to validate performance gains

**DON'T:**
- Use spans everywhere without measuring actual performance benefits
- Return spans from methods that allocate new arrays

### 4. Null-Conditional Assignment

**DO:**
- Use null-conditional assignment to simplify optional configuration logic
- Combine with null-coalescing operators for cleaner code

**DON'T:**
- Use when you need different behavior based on null vs. non-null
- Forget that the right-hand side is still evaluated (avoid side effects)

## Migration Considerations

### From C# 13 to C# 14

When updating existing code to use C# 14 features:

1. **Identify opportunities**: Search for patterns that C# 14 features simplify
2. **Refactor incrementally**: Update code in focused, reviewable chunks
3. **Test thoroughly**: Ensure behavior is preserved after refactoring
4. **Update documentation**: Keep specs and ADRs current

### Backward Compatibility

C# 14 features are purely additive. Code written in C# 13 continues to work in C# 14 without changes.

## Testing Recommendations

When using C# 14 features in ancplua-mcp:

1. **Extension Members**: Test that extensions are discoverable and work correctly with inheritance
2. **Field Keyword**: Verify validation logic executes as expected
3. **Span Conversions**: Add performance benchmarks for span-based operations
4. **Null-Conditional Assignment**: Test both null and non-null paths

## References

- [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [Extension Members Specification](https://github.com/dotnet/csharplang/blob/main/proposals/extensions.md)
- [Span<T> and Memory<T> Usage Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)

## Related Decisions

- ADR-001: Use .NET 10 as Target Framework
- Spec-001: MCP Protocol Implementation

## Version History

| Version | Date       | Changes                           |
|---------|------------|-----------------------------------|
| 1.0     | 2025-11-22 | Initial C# 14 features reference |

---

**Template Version**: 1.0
**Last Updated**: 2025-11-22
