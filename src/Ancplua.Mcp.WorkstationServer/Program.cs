using Ancplua.Mcp.CoreTools.Tools;
using Ancplua.Mcp.DebugTools;
using Ancplua.Mcp.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder(args);

// Apply standardized service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Add MCP server with stdio transport and explicit tool registration
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<FileSystemTools>()
    .WithTools<GitTools>()
    .WithTools<CiTools>()
    .WithTools<DebugTools>();

var app = builder.Build();

await app.RunAsync();
