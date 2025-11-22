using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;

// WorkstationServer - MCP Server for stdio communication
// Exposes filesystem, git, and CI tools to MCP clients like Claude Desktop

var builder = Host.CreateApplicationBuilder(args);

// Configure MCP server with stdio transport and auto-discover tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

await app.RunAsync();
