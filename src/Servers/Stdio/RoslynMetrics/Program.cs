using Ancplua.Mcp.Infrastructure.ServiceDefaults;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// MSBuildLocator MUST be registered before any code that references Roslyn/MSBuild types
if (!MSBuildLocator.IsRegistered)
{
    MSBuildLocator.RegisterDefaults();
}

var builder = Host.CreateApplicationBuilder(args);

// Apply standardized service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Add MCP server with stdio transport and auto-discover tools from this assembly
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

await app.RunAsync().ConfigureAwait(false);
