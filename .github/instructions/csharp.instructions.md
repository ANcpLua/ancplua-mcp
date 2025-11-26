---
applyTo: "**/*.cs"
description: "C# 14 and .NET 10 coding standards for ancplua-mcp"
---

# C# 14 / .NET 10 Coding Standards

## C# 14 Features - Use Them

### `field` Keyword in Properties

```csharp
// GOOD: C# 14 field keyword
public required string Name { get; init => field = value?.Trim() ?? throw new ArgumentNullException(); }

// BAD: Backing field pattern
private string _name;
public required string Name { get => _name; init => _name = value?.Trim() ?? throw new ArgumentNullException(); }
```

### Collection Expressions

```csharp
// GOOD
int[] numbers = [1, 2, 3];
List<string> items = ["a", "b", "c"];

// BAD
int[] numbers = new int[] { 1, 2, 3 };
List<string> items = new List<string> { "a", "b", "c" };
```

### Primary Constructors

```csharp
// GOOD
public class NuGetTools(ILogger<NuGetTools> logger) { }

// BAD (unless parameter needed elsewhere)
public class NuGetTools
{
    private readonly ILogger<NuGetTools> _logger;
    public NuGetTools(ILogger<NuGetTools> logger) => _logger = logger;
}
```

## Async Patterns

### ConfigureAwait in Library Code

```csharp
// GOOD: Library code
var result = await SomeAsync().ConfigureAwait(false);

// BAD: Missing ConfigureAwait
var result = await SomeAsync();
```

### CancellationToken Propagation

```csharp
// GOOD
public async Task<T> MyMethod(CancellationToken cancellationToken = default)
{
    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
}

// BAD
public async Task<T> MyMethod()
{
    await Task.Delay(100);
}
```

## CA Analyzer Compliance

| Rule | Requirement |
|------|-------------|
| CA1002 | Use `IReadOnlyList<T>` not `List<T>` in public APIs |
| CA1062 | Validate arguments of public methods |
| CA1305 | Specify `IFormatProvider` (use `CultureInfo.InvariantCulture`) |
| CA1307 | Specify `StringComparison` |
| CA1812 | Suppress for DI/reflection instantiated classes |
| CA1822 | Make methods static if no instance data accessed |
| CA1848 | Use `[LoggerMessage]` for high-perf logging |
| CA2007 | Use `ConfigureAwait(false)` |

## Naming Conventions

- PascalCase for types, methods, properties, events
- camelCase for parameters, local variables
- _camelCase for private fields
- IPascalCase for interfaces
- TPascalCase for type parameters

## Error Handling

```csharp
// GOOD: Specific exceptions
throw new ArgumentNullException(nameof(code));
throw new InvalidOperationException("Cannot analyze empty code");

// BAD: Generic exceptions
throw new Exception("Error");
```

## Nullable Reference Types

- Enabled throughout codebase
- Use `is null` / `is not null` checks
- Avoid `!` null-forgiving operator unless necessary
