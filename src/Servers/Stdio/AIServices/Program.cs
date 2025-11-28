using Ancplua.Mcp.Servers.Stdio.AIServices.Tools;
using Ancplua.Mcp.Infrastructure.ServiceDefaults;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Apply standardized service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Configure HttpClientFactory for GitHub API calls
builder.Services.AddHttpClient("GitHubApi", client =>
{
    client.BaseAddress = new Uri("https://api.github.com");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ancplua-mcp-ai-services", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
});

// Add MCP server with stdio transport and explicit tool registration
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ServiceDiscoveryTools>();

await builder.Build().RunAsync().ConfigureAwait(false);
