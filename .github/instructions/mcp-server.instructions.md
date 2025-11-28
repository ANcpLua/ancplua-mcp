---
applyTo: "**/Tools/**/*.cs, **/Program.cs"
description: "MCP Server development patterns using C# SDK - Autonomous agent mode"
---

# MCP Server Development

## Autonomous Agent Guidelines

When developing or modifying MCP tools:

1. **Act decisively** - Implement fixes without asking for permission
2. **Validate** - Run `dotnet build && dotnet test` before completing
3. **Document** - Update CHANGELOG.md and XML docs for new tools
4. **Create PRs** - Use `gh pr create` to submit autonomous fix PRs

## Tool Definition

### Required Attributes

```csharp
[McpServerToolType]
public class MyTools
{
    [McpServerTool]
    [Description("Clear, actionable description of what this tool does")]
    public async Task<ResultDto> MyTool(
        [Description("Parameter purpose")] string param,
        CancellationToken cancellationToken = default)
    {
        // Return structured DTO, not string
    }
}
```

### Tool Naming

- Tool names are **public API** - changes require ADR
- Use PascalCase for tool methods
- Use camelCase for parameters
- Be descriptive: `AnalyzeCSharp` not `Analyze`

### Return Types

```csharp
// GOOD: Structured DTO
return new { symbol = name, complexity = 10, maintainability = 85 };

// BAD: Free-form string
return "The complexity is 10 and maintainability is 85";
```

## Server Setup

### Basic Stdio Server

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
    options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

### HTTP Server

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
var app = builder.Build();
app.MapMcp();
await app.RunAsync();
```

## Dependency Injection

```csharp
[McpServerTool]
[Description("Fetches data from a URL")]
public static async Task<string> FetchData(
    HttpClient httpClient,  // Injected
    [Description("The URL to fetch")] string url,
    CancellationToken cancellationToken) =>
    await httpClient.GetStringAsync(url, cancellationToken);
```

## Error Handling

```csharp
// Use McpProtocolException for protocol errors
throw new McpProtocolException(McpErrorCode.InvalidParams, "Parameter 'code' cannot be empty");
```

## Logging Discipline

- stdout = MCP protocol only
- stderr = logs and diagnostics
- Use `LogToStandardErrorThreshold = LogLevel.Trace`
