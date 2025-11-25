using Ancplua.Mcp.CoreTools.Tools;
using Ancplua.Mcp.DebugTools;
using Ancplua.Mcp.ServiceDefaults;

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
    .WithTools<DebugTools>();

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
