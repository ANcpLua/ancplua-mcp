using Ancplua.Mcp.CoreTools.Tools;
using Ancplua.Mcp.DebugTools;
using Ancplua.Mcp.ServiceDefaults;
using Ancplua.Mcp.WhisperMesh.Client;
using Ancplua.Mcp.WhisperMesh.Services;
using Ancplua.Mcp.WhisperMesh.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder(args);

// Apply standardized service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Add WhisperMesh services
builder.Services.Configure<WhisperMeshClientOptions>(
    builder.Configuration.GetSection(WhisperMeshClientOptions.SectionName));
builder.Services.AddSingleton<IWhisperMeshClient, NatsWhisperMeshClient>();
builder.Services.AddSingleton<WhisperAggregator>();

// Add MCP server with stdio transport and explicit tool registration
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<FileSystemTools>()
    .WithTools<GitTools>()
    .WithTools<CiTools>()
    .WithTools<DebugToolset>()
    .WithTools<WhisperAggregatorTools>();

var app = builder.Build();

await app.RunAsync().ConfigureAwait(false);
