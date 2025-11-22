using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace Ancplua.Mcp.Testing;

/// <summary>
/// Provides helper methods for testing MCP servers in-process.
/// </summary>
public class McpTestHost : IAsyncDisposable
{
    private readonly IHost _host;
    private bool _disposed;

    private McpTestHost(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Creates a test host for an MCP server with stdio transport.
    /// </summary>
    /// <param name="configureServices">Optional action to configure additional services.</param>
    /// <returns>An initialized MCP test host.</returns>
    public static async Task<McpTestHost> CreateStdioServerAsync(
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateApplicationBuilder();

        // Configure logging for tests
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddConsole();

        // Add MCP server with stdio transport
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(McpTestHost).Assembly);

        // Allow additional service configuration
        configureServices?.Invoke(builder.Services);

        var host = builder.Build();
        await host.StartAsync();

        return new McpTestHost(host);
    }

    /// <summary>
    /// Creates a test host for an MCP server with custom tool assemblies.
    /// </summary>
    /// <param name="toolAssembly">The assembly containing MCP tool types.</param>
    /// <param name="configureServices">Optional action to configure additional services.</param>
    /// <returns>An initialized MCP test host.</returns>
    public static async Task<McpTestHost> CreateWithToolsAsync(
        System.Reflection.Assembly toolAssembly,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateApplicationBuilder();

        // Configure logging for tests
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddConsole();

        // Add MCP server with stdio transport and specified tools
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(toolAssembly);

        // Allow additional service configuration
        configureServices?.Invoke(builder.Services);

        var host = builder.Build();
        await host.StartAsync();

        return new McpTestHost(host);
    }

    /// <summary>
    /// Gets a service from the test host's service provider.
    /// </summary>
    /// <typeparam name="T">The service type to retrieve.</typeparam>
    /// <returns>The requested service instance.</returns>
    public T GetService<T>() where T : notnull
    {
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets the MCP server instance from the test host.
    /// </summary>
    /// <returns>The McpServer instance.</returns>
    public McpServer GetMcpServer()
    {
        return _host.Services.GetRequiredService<McpServer>();
    }

    /// <summary>
    /// Stops the test host.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_disposed)
        {
            await _host.StopAsync();
        }
    }

    /// <summary>
    /// Disposes the test host and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await StopAsync();

        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _host.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Extension methods for MCP testing.
/// </summary>
public static class McpTestExtensions
{
    /// <summary>
    /// Creates a test logger factory for MCP testing.
    /// </summary>
    /// <returns>A configured ILoggerFactory instance.</returns>
    public static ILoggerFactory CreateTestLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });
    }
}
