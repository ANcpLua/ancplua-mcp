using Ancplua.Mcp.Libraries.CoreTools.Tools;
using Ancplua.Mcp.Libraries.DebugTools;
using Ancplua.Mcp.Infrastructure.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Apply standardized service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// OpenAPI (optional, dev only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

// Required for DebugTools HTTP context inspection
builder.Services.AddHttpContextAccessor();

// Add MCP server with HTTP transport and explicit tool registration
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<FileSystemTools>()
    .WithTools<GitTools>()
    .WithTools<CiTools>()
    .WithTools<DebugToolset>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map MCP endpoints
app.MapMcp();

// Map default health endpoints
app.MapDefaultEndpoints();

app.Run();
