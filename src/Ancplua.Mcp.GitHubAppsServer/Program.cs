using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Ancplua.Mcp.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

// Add environment variables for API keys (JULES_API_KEY, GITHUB_TOKEN, etc.)
builder.Configuration.AddEnvironmentVariables();

// Apply standardized service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Add MCP server with stdio transport and auto-discover tools from this assembly
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

await app.RunAsync();
