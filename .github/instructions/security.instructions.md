---
applyTo: "**/*.cs"
description: "Security and OWASP compliance rules"
---

# Security Requirements

## Path Traversal Prevention

```csharp
// GOOD: Validate path is within allowed directory
var fullPath = Path.GetFullPath(userPath);
if (!fullPath.StartsWith(allowedBasePath, StringComparison.OrdinalIgnoreCase))
    throw new UnauthorizedAccessException("Path outside allowed directory");
File.ReadAllText(fullPath);

// BAD: Direct path usage
File.ReadAllText(userPath);
```

## Command Injection Prevention

```csharp
// GOOD: Use argument arrays
Process.Start("git", ["add", "--", fileName]);

// BAD: String concatenation
Process.Start($"git add {fileName}");
```

## No Hardcoded Secrets

```csharp
// GOOD: Environment variable
var apiKey = Environment.GetEnvironmentVariable("API_KEY")
    ?? throw new InvalidOperationException("API_KEY not configured");

// BAD: Hardcoded
var apiKey = "sk-1234567890";
```

## SQL Injection Prevention

```csharp
// GOOD: Parameterized query
cmd.CommandText = "SELECT * FROM Users WHERE Id = @id";
cmd.Parameters.AddWithValue("@id", userId);

// BAD: String concatenation
cmd.CommandText = $"SELECT * FROM Users WHERE Id = {userId}";
```

## Input Validation

```csharp
// GOOD: Validate early
public void ProcessFile(string path)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    if (path.Length > 260)
        throw new ArgumentException("Path too long", nameof(path));
    // ...
}
```

## Cryptography

- Use Argon2 or bcrypt for password hashing
- Use AES-256 for encryption
- Never roll your own crypto

## Session Security

- Configure cookies with `HttpOnly`, `Secure`, `SameSite=Strict`
- Generate new session IDs on login
- Implement rate limiting

## OWASP Top 10 Checklist

1. Injection - Use parameterized queries
2. Broken Auth - Validate all auth paths
3. Sensitive Data - Encrypt at rest and in transit
4. XXE - Disable external entities
5. Access Control - Deny by default
6. Misconfiguration - No debug in prod
7. XSS - Context-aware encoding
8. Insecure Deserialization - Validate types
9. Vulnerable Dependencies - Keep updated
10. Logging - Log security events
