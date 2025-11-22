using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Configure MCP server with HTTP transport and auto-discover tools
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map MCP endpoints
app.MapMcp();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", server = "HttpServer MCP" })
    .WithName("HealthCheck");

app.Run();
