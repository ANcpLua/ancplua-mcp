using Ancplua.Mcp.HttpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// MCP HTTP endpoints
app.MapGet("/mcp/tools", () =>
{
    return new
    {
        tools = new[]
        {
            new { name = "filesystem", description = "Filesystem operations" },
            new { name = "git", description = "Git repository operations" },
            new { name = "ci", description = "CI/CD and build operations" }
        }
    };
})
.WithName("ListTools");

app.MapGet("/health", () => new { status = "healthy", server = "HttpServer MCP" })
    .WithName("HealthCheck");

app.Run();
