using System.Reflection.PortableExecutable;
using System.Security;
using System.Text.Json;

namespace Ancplua.Mcp.DebugTools.Tools;

public static class McpServer
{
    public static async Task RunAsync(CancellationToken ct)
    {
        // MCP uses JSON-RPC over stdio
        while (!ct.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null) break;

            var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);

            // Handle request and generate response asynchronously
            var response = await HandleRequestAsync(request, ct).ConfigureAwait(false);

            // Serialize and write response
            var responseJson = JsonSerializer.Serialize(response);
            await Console.Out.WriteLineAsync(responseJson).ConfigureAwait(false);
        }
    }

    private static async Task<object> HandleRequestAsync(JsonRpcRequest? request, CancellationToken ct)
    {
        return request?.Method switch
        {
            "tools/list" => await ListToolsAsync(ct).ConfigureAwait(false),
            "tools/call" => await CallToolAsync(request.Params, ct).ConfigureAwait(false),
            _ => ErrorResponse(request?.Id, "Unknown method")
        };
    }

    private static Task<object> ListToolsAsync(CancellationToken ct)
    {
        // Check cancellation before creating response
        ct.ThrowIfCancellationRequested();

        var response = new
        {
            tools = new[]
            {
                new
                {
                    name = "inspect_binary",
                    description = "Inspect .NET assembly metadata",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            path = new { type = "string", description = "Path to .dll or .exe" }
                        },
                        required = new[] { "path" }
                    }
                }
            }
        };

        return Task.FromResult<object>(response);
    }

    private static async Task<object> CallToolAsync(JsonElement? @params, CancellationToken ct)
    {
        var path = @params?.GetProperty("arguments").GetProperty("path").GetString();
        if (path is null)
        {
            return ErrorResponse(null, "Missing path");
        }

        try
        {
            // Offload blocking I/O and CPU-intensive work to thread pool
            var (classification, report) = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var classificationResult = BinaryInspector.Inspect(path);
                var reportResult = BinaryInspector.FormatReport(classificationResult, path);
                return (classificationResult, reportResult);
            }, ct).ConfigureAwait(false);

            return new { content = new[] { new { type = "text", text = report } } };
        }
        catch (OperationCanceledException)
        {
            return ErrorResponse(null, "Operation cancelled");
        }
        catch (FileNotFoundException ex)
        {
            return ErrorResponse(null, $"File not found: {ex.FileName}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ErrorResponse(null, $"Access denied: {ex.Message}");
        }
        catch (SecurityException ex)
        {
            return ErrorResponse(null, $"Security error: {ex.Message}");
        }
        catch (IOException ex)
        {
            return ErrorResponse(null, $"IO error: {ex.Message}");
        }
        catch (BadImageFormatException ex)
        {
            return ErrorResponse(null, $"Invalid binary format: {ex.Message}");
        }
    }

    private static object ErrorResponse(object? id, string message) => new
    {
        id, error = new { code = -32000, message }
    };

#pragma warning disable CA1812 // Type is instantiated via JSON deserialization
    private sealed record JsonRpcRequest(object? Id, string? Method, JsonElement? Params);
#pragma warning restore CA1812
}
